using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Command;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using McdfExporter.Services;
using McdfExporter.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace McdfExporter
{
    public sealed class Plugin : IDalamudPlugin
    {
        [PluginService] internal static IFramework Framework { get; private set; } = null!;
        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] internal static IClientState ClientState { get; private set; } = null!;
        [PluginService] internal static IPluginLog Log { get; private set; } = null!;
        [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
        [PluginService] internal static ITargetManager TargetManager { get; private set; } = null!;

        private const string CommandName = "/mcdfexport";

        public readonly WindowSystem WindowSystem = new("McdfExporter");
        public readonly FileDialogManager FileDialogManager = new();

        public readonly IpcManager IpcManager;
        public readonly RegistrationService RegistrationService;
        private readonly McdfApplicationService _mcdfApplier;
        private readonly MainWindow _mainWindow;

        private readonly Stopwatch _updateStopwatch = new();
        private const long UpdateIntervalMs = 250;

        private class ActiveMcdfApplication
        {
            public Guid PenumbraCollectionId { get; set; }
            public int ObjectIndex { get; set; }
            public string CharacterName { get; set; } = string.Empty;
        }

        private readonly Dictionary<string, ActiveMcdfApplication> _activeApplications = new();
        private readonly HashSet<string> _processingCharacters = new HashSet<string>();

        public Plugin()
        {
            IpcManager = new IpcManager(PluginInterface);
            RegistrationService = new RegistrationService();
            _mcdfApplier = new McdfApplicationService(IpcManager, Framework);

            var charaDataFactory = new CharacterDataFactory(IpcManager);
            var charaFileHandler = new CharaDataFileHandler(charaDataFactory);

            _mainWindow = new MainWindow(this, charaFileHandler, _mcdfApplier);

            WindowSystem.AddWindow(_mainWindow);
            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) { HelpMessage = "Opens the MCDF Exporter window." });
            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

            Framework.Update += OnFrameworkUpdate;
            _updateStopwatch.Start();
        }

        public void Dispose()
        {
            Framework.Update -= OnFrameworkUpdate;
            WindowSystem.RemoveAllWindows();
            CommandManager.RemoveHandler(CommandName);
            PluginInterface.UiBuilder.Draw -= DrawUI;
            PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUI;

            foreach (var app in _activeApplications.Values.ToList())
            {
                CleanupApplication(app);
            }
            _activeApplications.Clear();

            IpcManager.Dispose();
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            if (_updateStopwatch.ElapsedMilliseconds < UpdateIntervalMs) return;
            _updateStopwatch.Restart();

            IpcManager.CheckApis();
            if (!IpcManager.AllApisAvailable()) return;

            var keysToCleanUp = new List<string>();
            foreach (var (key, app) in _activeApplications)
            {
                if (_processingCharacters.Contains(key)) continue;

                var obj = ObjectTable[app.ObjectIndex];
                if (obj is not IPlayerCharacter player || !player.IsValid() || player.Name.TextValue != app.CharacterName)
                {
                    keysToCleanUp.Add(key);
                }
            }

            foreach (var key in keysToCleanUp)
            {
                if (_activeApplications.Remove(key, out var app))
                {
                    CleanupApplication(app);
                }
            }

            var onScreenCharacters = ObjectTable
                .OfType<IPlayerCharacter>()
                .Where(p => p.IsValid() && p != ClientState.LocalPlayer && p.HomeWorld.RowId != 0);

            foreach (var character in onScreenCharacters)
            {
                var worldName = character.HomeWorld.Value.Name.ToString();
                var key = $"{character.Name.TextValue.ToLowerInvariant()}@{worldName.ToLowerInvariant()}";

                if (!_activeApplications.ContainsKey(key) && !_processingCharacters.Contains(key))
                {
                    var registered = RegistrationService.GetRegisteredCharacter(character.Name.TextValue, worldName);
                    if (registered != null)
                    {
                        ApplyMcdfToCharacter(key, character, registered.McdfFilePath);
                    }
                }
            }
        }

        private void CleanupApplication(ActiveMcdfApplication app)
        {
            Log.Info($"Cleaning up MCDF for {app.CharacterName} (Index: {app.ObjectIndex})");
            IpcManager.CustomizePlus.RemoveTemporaryProfile(app.ObjectIndex);
            IpcManager.Glamourer.RevertState(app.ObjectIndex);
            if (app.PenumbraCollectionId != Guid.Empty)
            {
                IpcManager.Penumbra.RemoveTemporaryCollection(app.PenumbraCollectionId);
            }
        }

        private void ApplyMcdfToCharacter(string key, IPlayerCharacter character, string mcdfPath)
        {
            _processingCharacters.Add(key);

            if (!_activeApplications.TryAdd(key, new ActiveMcdfApplication { ObjectIndex = character.ObjectIndex, CharacterName = character.Name.TextValue }))
            {
                _processingCharacters.Remove(key);
                return;
            }

            Log.Info($"[Registration] Applying MCDF to {character.Name}.");

            _ = Task.Run(async () =>
            {
                try
                {
                    var collectionId = await _mcdfApplier.ApplyMcdf(character, mcdfPath);
                    if (collectionId.HasValue)
                    {
                        if (_activeApplications.TryGetValue(key, out var app))
                        {
                            app.PenumbraCollectionId = collectionId.Value;
                        }
                        else
                        {
                            Log.Warning($"Character {key} disappeared during MCDF application. Cleaning up generated collection.");
                            IpcManager.Penumbra.RemoveTemporaryCollection(collectionId.Value);
                        }
                    }
                    else
                    {
                        Log.Error($"Failed to apply MCDF for {key}. Removing from active list.");
                        await Framework.RunOnFrameworkThread(() => _activeApplications.Remove(key));
                    }
                }
                finally
                {
                    _processingCharacters.Remove(key);
                }
            });
        }

        private void OnCommand(string command, string args) => ToggleMainUI();
        private void DrawUI() { WindowSystem.Draw(); FileDialogManager.Draw(); }
        public void ToggleMainUI() => _mainWindow.Toggle();
    }
}
