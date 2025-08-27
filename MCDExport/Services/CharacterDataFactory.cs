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

        var penumbraMods = IpcManager.Penumbra.GetCharacterData(player.ObjectIndex);
        if (penumbraMods == null)
        {
            Plugin.Log.Error("Failed to get Penumbra data. Aborting export.");
            return null;
        }

        var fileReplacements = new List<FileReplacement>();
        var hashCache = new Dictionary<string, string>();

        Plugin.Log.Info($"Found {penumbraMods.Count} potential mod associations from Penumbra. Resolving...");

        foreach (var (resolvedPath, gamePaths) in penumbraMods)
        {
            if (string.IsNullOrEmpty(resolvedPath) || !File.Exists(resolvedPath) || gamePaths.Count == 0)
            {
                Plugin.Log.Warning($"Could not resolve game paths for: '{string.Join(", ", gamePaths)}'");
                continue;
            }

            var hash = await Task.Run(() => FileHasher.GetFileHash(resolvedPath, hashCache));
            var length = (int)new FileInfo(resolvedPath).Length;

            fileReplacements.Add(new FileReplacement
            {
                Hash = hash,
                GamePaths = gamePaths.ToList(),
                Length = length,
                LocalPath = resolvedPath
            });
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
