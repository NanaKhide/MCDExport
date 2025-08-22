using Dalamud.Game.ClientState.Objects.Types;
using McdfExporter.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace McdfExporter.Services;

public class CharacterDataFactory
{
    public IpcManager IpcManager { get; }

    public CharacterDataFactory(IpcManager ipcManager)
    {
        IpcManager = ipcManager;
    }

    public async Task<McdfExporter.Data.CharacterData?> CreateCharacterData()
    {
        var player = Plugin.ClientState.LocalPlayer;
        if (player == null) return null;

        var glamourerData = IpcManager.Glamourer.GetStateBase64(player.ObjectIndex);
        if (string.IsNullOrEmpty(glamourerData))
        {
            Plugin.Log.Error("Failed to get Glamourer data. Aborting export.");
            return null;
        }

        var penumbraResourcePaths = IpcManager.Penumbra.GetGameObjectResourcePaths(player.ObjectIndex);
        var fileReplacements = new List<FileReplacement>();
        var modDirectory = IpcManager.Penumbra.GetModDirectory();

        if (string.IsNullOrEmpty(modDirectory))
        {
            Plugin.Log.Error("Could not get Penumbra's mod directory. Aborting export.");
            return null;
        }

        Plugin.Log.Info($"Penumbra Mod Directory: {modDirectory}");
        Plugin.Log.Info($"Found {penumbraResourcePaths.Length} resource path dictionaries from Penumbra.");

        foreach (var resourceDict in penumbraResourcePaths)
        {
            foreach (var (gamePath, localPaths) in resourceDict)
            {
                Plugin.Log.Debug($"Processing game path: {gamePath}");
                if (!localPaths.Any()) continue;

                string? resolvedPath = null;

                var normalizedGamePath = gamePath.Replace('/', '\\');
                if (File.Exists(normalizedGamePath))
                {
                    resolvedPath = normalizedGamePath;
                    Plugin.Log.Info($"  -> Found valid absolute path from game path: {resolvedPath}");
                }
                else
                {
                    foreach (var path in localPaths)
                    {
                        var normalizedPath = path.Replace('/', '\\');

                        if (File.Exists(normalizedPath))
                        {
                            resolvedPath = normalizedPath;
                            Plugin.Log.Info($"  -> Found valid absolute path: {resolvedPath}");
                            break;
                        }

                        var combinedPath = Path.Combine(modDirectory, normalizedPath);
                        if (File.Exists(combinedPath))
                        {
                            resolvedPath = combinedPath;
                            Plugin.Log.Info($"  -> Found valid combined path: {resolvedPath}");
                            break;
                        }
                    }
                }

                if (resolvedPath == null)
                {
                    Plugin.Log.Warning($"Could not find a valid local file for game path '{gamePath}' from any of the {localPaths.Count + 1} options provided by Penumbra.");
                    continue;
                }

                var hash = await Task.Run(() => FileHasher.GetFileHash(resolvedPath));
                var length = (int)new FileInfo(resolvedPath).Length;

                var existingReplacement = fileReplacements.FirstOrDefault(f => f.Hash == hash);
                if (existingReplacement != null)
                {
                    existingReplacement.GamePaths.Add(gamePath);
                }
                else
                {
                    fileReplacements.Add(new FileReplacement
                    {
                        Hash = hash,
                        GamePaths = new List<string> { gamePath },
                        Length = length,
                        LocalPath = resolvedPath
                    });
                }
            }
        }

        Plugin.Log.Info($"Added {fileReplacements.Count} file replacements to the MCDF file.");

        return new McdfExporter.Data.CharacterData
        {
            GlamourerData = glamourerData,
            FileReplacements = fileReplacements,
            ManipulationData = IpcManager.Penumbra.GetPlayerMetaManipulations(),
            CustomizePlusData = IpcManager.CustomizePlus.GetBodyScale(player) ?? string.Empty
        };
    }
}
