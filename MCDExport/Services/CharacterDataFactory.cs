using McdfExporter.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace McdfExporter.Services;

public class CharacterDataFactory
{
    public IpcManager IpcManager { get; }

    public CharacterDataFactory(IpcManager ipcManager)
    {
        IpcManager = ipcManager;
    }

    public async Task<CharacterData?> CreateCharacterData(ExportProgress progress)
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

        Plugin.Log.Verbose($"Found {penumbraMods.Count} potential mod associations from Penumbra. Resolving...");

        var validMods = penumbraMods
            .Where(mod => !string.IsNullOrEmpty(mod.Key) && File.Exists(mod.Key) && mod.Value.Any())
            .ToList();
        progress.Message = "Processing mod files...";
        progress.TotalFiles = validMods.Count;
        progress.FilesProcessed = 0;
        var processingTasks = validMods.Select(async mod =>
        {
            var (resolvedPath, gamePaths) = mod;
            var hash = await Task.Run(() => FileHasher.GetFileHash(resolvedPath));
            var length = (int)new FileInfo(resolvedPath).Length;

            Interlocked.Increment(ref progress.FilesProcessed);

            return new FileReplacement
            {
                Hash = hash,
                GamePaths = gamePaths.ToList(),
                Length = length,
                LocalPath = resolvedPath
            };
        }).ToList();

        var fileReplacements = (await Task.WhenAll(processingTasks)).ToList();

        Plugin.Log.Verbose($"Added {fileReplacements.Count} unique mod files to the MCDF file.");

        return new McdfExporter.Data.CharacterData
        {
            GlamourerData = glamourerData,
            FileReplacements = fileReplacements,
            ManipulationData = IpcManager.Penumbra.GetPlayerMetaManipulations(),
            CustomizePlusData = IpcManager.CustomizePlus.GetBodyScale(player) ?? string.Empty
        };
    }
}
