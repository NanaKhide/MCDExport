using Dalamud.Game.Command;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using McdfExporter.Services;
using McdfExporter.Windows;

namespace McdfExporter;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;

    private const string CommandName = "/mcdfexport";

    public readonly WindowSystem WindowSystem = new("McdfExporter");
    public readonly FileDialogManager FileDialogManager = new();

    public readonly IpcManager IpcManager;
    public readonly CharacterDataFactory CharacterDataFactory;
    public readonly CharaDataFileHandler CharaDataFileHandler;

    private readonly MainWindow _mainWindow;

    public Plugin()
    {
        IpcManager = new IpcManager();
        CharacterDataFactory = new CharacterDataFactory(IpcManager);
        CharaDataFileHandler = new CharaDataFileHandler(CharacterDataFactory);

        _mainWindow = new MainWindow(this);
        WindowSystem.AddWindow(_mainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the MCDF Exporter window."
        });

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        CommandManager.RemoveHandler(CommandName);
        PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUI;
        IpcManager.Dispose();
    }

    private void OnCommand(string command, string args) => ToggleMainUI();
    private void DrawUI()
    {
        WindowSystem.Draw();
        FileDialogManager.Draw();
    }
    public void ToggleMainUI() => _mainWindow.Toggle();
}
