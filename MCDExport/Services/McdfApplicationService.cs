using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using McdfExporter.Extensions;

namespace McdfExporter.Services
{
    public class McdfApplicationService
    {
        private readonly IpcManager _ipcManager;
        private readonly string _baseTempPath;
        private readonly IFramework _framework;

        public McdfApplicationService(IpcManager ipcManager, IFramework framework)
        {
            _ipcManager = ipcManager;
            _framework = framework;
            _baseTempPath = Path.Combine(Path.GetTempPath(), "McdfExporter");
            Directory.CreateDirectory(_baseTempPath);
        }

        public async Task<Guid?> ApplyMcdf(IGameObject character, string mcdfPath)
        {
            Guid collectionId = Guid.Empty;
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                using var reader = McdfReader.FromPath(mcdfPath);
                if (reader == null)
                {
                    Plugin.Log.Error("Failed to read MCDF file.");
                    return null;
                }

                var data = reader.GetData();
                var extractedFiles = reader.ExtractFiles(tempDir);
                // timing changes cause stuff doesnt like how i do it
                // penumbra on background thread first
                collectionId = _ipcManager.Penumbra.CreateTemporaryCollection($"MCDF_{character.Name}_{character.GameObjectId}");
                Plugin.Log.Info($"Created temporary collection {collectionId} for {character.Name}");
                _ipcManager.Penumbra.AssignTemporaryCollection(collectionId, character.ObjectIndex, true);
                _ipcManager.Penumbra.AddTemporaryMod("MCDF_Files", collectionId, extractedFiles, data.ManipulationData);
                Plugin.Log.Info($"Applied Penumbra mods for {character.Name}");

                // REVERT THE CHARACTER FIRST
                // also on main thread cause.... Idk man something crashed me
                await _framework.RunOnFrameworkThread(() =>
                {
                    _ipcManager.Glamourer.RevertState(character.ObjectIndex);
                    _ipcManager.CustomizePlus.RemoveTemporaryProfile(character.ObjectIndex);
                    Plugin.Log.Info($"Reverted character {character.Name} to base state.");
                });

                // let glamourer cook
                await Task.Delay(100);

                //apply glamourer and customize after
                await _framework.RunOnFrameworkThread(() =>
                {
                    _ipcManager.Glamourer.ApplyState(data.GlamourerData, character.ObjectIndex);
                    Plugin.Log.Info($"Applied Glamourer state for {character.Name}");

                    if (character is ICharacter chara)
                    {
                        _ipcManager.CustomizePlus.SetTemporaryProfile(chara.ObjectIndex, data.CustomizePlusData);
                        Plugin.Log.Info($"Applied Customize+ profile for {character.Name}");
                    }
                });

                // let everything else cook and redraw after
                await Task.Delay(100);

                await RedrawAndWait(character);

                return collectionId;
            }
            catch (Exception e)
            {
                Plugin.Log.Error(e, $"An error occurred during MCDF application for {character.Name}.");
                if (collectionId != Guid.Empty)
                {
                    _ipcManager.Penumbra.RemoveTemporaryCollection(collectionId);
                }
                return null;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.Warning(ex, "Failed to clean up temporary MCDF directory.");
                }
            }
        }

        private async Task RedrawAndWait(IGameObject actor)
        {
            Plugin.Log.Info($"Requesting redraw for {actor.Name}...");
            _ipcManager.Penumbra.RedrawActor(actor.ObjectIndex);

            var start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 5)
            {
                var isDrawing = await _framework.RunOnFrameworkThread(() => actor.IsDrawing());
                if (isDrawing)
                {
                    Plugin.Log.Info($"{actor.Name} is drawing. Redraw successful.");
                    await Task.Delay(1000);
                    return;
                }
                await Task.Delay(100);
            }

            Plugin.Log.Warning($"Timed out waiting for '{actor.Name}' to redraw!");
        }
    }
}
