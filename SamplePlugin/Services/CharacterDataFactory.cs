using Dalamud.Game.ClientState.Objects.Types;
using McdfExporter.Data;
using System;
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

        var penumbraMods = IpcManager.Penumbra.GetGameObjectResourcePaths(player.ObjectIndex);
        var fileReplacements = new List<FileReplacement>();

        Plugin.Log.Info($"Found {penumbraMods.Count} potential mod associations from Penumbra. Resolving...");

        foreach (var (key, value) in penumbraMods)
        {
            await Task.Delay(1);

            string? resolvedPath = null;
            string? gamePath = null;

            var normalizedKey = key.Replace('/', '\\');
            var normalizedValue = value.Replace('/', '\\');

            if (Path.IsPathRooted(normalizedKey) && !Path.IsPathRooted(normalizedValue) && File.Exists(normalizedKey))
            {
                resolvedPath = normalizedKey;
                gamePath = normalizedValue;
                Plugin.Log.Info($"Found mod file via absolute key for game path '{gamePath}' -> '{resolvedPath}'");
            }
            else if (Path.IsPathRooted(normalizedValue) && !Path.IsPathRooted(normalizedKey) && File.Exists(normalizedValue))
            {
                resolvedPath = normalizedValue;
                gamePath = normalizedKey;
                Plugin.Log.Info($"Found mod file via absolute value for game path '{gamePath}' -> '{resolvedPath}'");
            }
            else
            {
                
                var resolvedFromKey = IpcManager.Penumbra.ResolvePlayerPath(normalizedKey);
                if (!string.IsNullOrEmpty(resolvedFromKey) && !resolvedFromKey.Equals(normalizedKey, StringComparison.OrdinalIgnoreCase) && File.Exists(resolvedFromKey))
                {
                    resolvedPath = resolvedFromKey;
                    gamePath = normalizedKey;
                    Plugin.Log.Info($"Found mod file by resolving key for game path '{gamePath}' -> '{resolvedPath}'");
                }
                else
                {
                    var resolvedFromValue = IpcManager.Penumbra.ResolvePlayerPath(normalizedValue);
                    if (!string.IsNullOrEmpty(resolvedFromValue) && !resolvedFromValue.Equals(normalizedValue, StringComparison.OrdinalIgnoreCase) && File.Exists(resolvedFromValue))
                    {
                        resolvedPath = resolvedFromValue;
                        gamePath = normalizedValue;
                        Plugin.Log.Info($"Found mod file by resolving value for game path '{gamePath}' -> '{resolvedPath}'");
                    }
                }
            }

            if (string.IsNullOrEmpty(resolvedPath) || string.IsNullOrEmpty(gamePath))
            {
                Plugin.Log.Warning($"Could not resolve mod file from pair: (Key: '{key}', Value: '{value}')");
                continue;
            }

            var hash = await Task.Run(() => FileHasher.GetFileHash(resolvedPath));
            var length = (int)new FileInfo(resolvedPath).Length;

            var existingReplacement = fileReplacements.FirstOrDefault(f => f.Hash == hash);
            if (existingReplacement != null)
            {
                if (!existingReplacement.GamePaths.Contains(gamePath))
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

        Plugin.Log.Info($"Added {fileReplacements.Count} unique mod files to the MCDF file.");

        return new McdfExporter.Data.CharacterData
        {
            GlamourerData = glamourerData,
            FileReplacements = fileReplacements,
            ManipulationData = IpcManager.Penumbra.GetPlayerMetaManipulations(),
            CustomizePlusData = IpcManager.CustomizePlus.GetBodyScale(player) ?? string.Empty
        };
    }
}
