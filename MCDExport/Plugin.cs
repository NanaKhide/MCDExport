using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using McdfExporter.Services;
using McdfExporter.Windows;

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
        public readonly EventManager EventManager;
        public readonly RegistrationService RegistrationService;
        private readonly McdfApplicationService _mcdfApplier;
        private readonly AutoApplyService _autoApplyService;
        private readonly MainWindow _mainWindow;

        public Plugin()
        {
            IpcManager = new IpcManager(PluginInterface);
            EventManager = new EventManager();
            RegistrationService = new RegistrationService(EventManager);
            _mcdfApplier = new McdfApplicationService(IpcManager, Framework);
            _autoApplyService = new AutoApplyService(Framework, TargetManager, ObjectTable, ClientState, Log, EventManager, RegistrationService, _mcdfApplier, IpcManager);

            var charaDataFactory = new CharacterDataFactory(IpcManager);
            var charaFileHandler = new CharaDataFileHandler(charaDataFactory);

            _mainWindow = new MainWindow(this, charaFileHandler, RegistrationService,_autoApplyService);

            WindowSystem.AddWindow(_mainWindow);

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) { HelpMessage = "Opens the MCDF Exporter window." });
            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
        }

        public void Dispose()
        {
            WindowSystem.RemoveAllWindows();
            CommandManager.RemoveHandler(CommandName);
            PluginInterface.UiBuilder.Draw -= DrawUI;
            PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUI;

            _autoApplyService.Dispose();
            IpcManager.Dispose();
        }

        private void OnCommand(string command, string args) => ToggleMainUI();
        private void DrawUI() { WindowSystem.Draw(); FileDialogManager.Draw(); }
        public void ToggleMainUI() => _mainWindow.Toggle();
    }
}
