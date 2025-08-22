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

        var penumbraMods = IpcManager.Penumbra.GetGameObjectResourcePaths(player.ObjectIndex);
        var fileReplacements = new List<FileReplacement>();
        var modDirectory = IpcManager.Penumbra.GetModDirectory();

        if (string.IsNullOrEmpty(modDirectory))
        {
            Plugin.Log.Error("Could not get Penumbra's mod directory. Aborting export.");
            return null;
        }

        foreach (var (gamePath, localPath) in penumbraMods)
        {
            var fullPath = Path.IsPathRooted(localPath) ? localPath : Path.Combine(modDirectory, localPath);

            if (!File.Exists(fullPath)) continue;

            var hash = await Task.Run(() => FileHasher.GetFileHash(fullPath));
            var length = (int)new FileInfo(fullPath).Length;

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
                    LocalPath = fullPath
                });
            }
        }

        return new McdfExporter.Data.CharacterData
        {
            GlamourerData = glamourerData,
            FileReplacements = fileReplacements,
            ManipulationData = IpcManager.Penumbra.GetPlayerMetaManipulations(),
            CustomizePlusData = IpcManager.CustomizePlus.GetBodyScale(player) ?? string.Empty
        };
    }
}
