using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;

namespace McdfExporter.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin _plugin;
    private bool _readExport;
    private string _exportDescription = string.Empty;
    private Task? _exportTask;

    public MainWindow(Plugin plugin) : base("MCDF Exporter", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(400, 250),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        _plugin = plugin;
    }

    public override void Draw()
    {
        ImGui.Text("Mare Character Data File Export");

        if (ImGui.CollapsingHeader("Help"))
        {
            ImGui.TextWrapped("This feature allows you to pack your character into a .mcdf file and manually send it to other people. " +
                              "Be aware that by sharing this file, you are giving away your current character appearance irrevocably.");
        }

        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Checkbox("##readExport", ref _readExport);
        ImGui.SameLine();
        ImGui.TextWrapped("I understand that by exporting my character data into a file and sending it to other people, I am giving away my current character appearance. " +
                          "People I share my data with have the ability to share it with other people without limitations.");

        ImGui.Spacing();

        var ipcReady = _plugin.IpcManager.IsIpcReady();
        if (!_readExport || !ipcReady)
        {
            ImGui.BeginDisabled();
        }

        ImGui.InputTextWithHint("##description", "Export Description (optional)", ref _exportDescription, 255);

        if (ImGui.Button("Export Character as MCDF"))
        {
            var defaultFileName = string.IsNullOrEmpty(_exportDescription)
                ? "export.mcdf"
                : string.Join("_", $"{_exportDescription}.mcdf".Split(Path.GetInvalidFileNameChars()));

            _plugin.FileDialogManager.SaveFileDialog("Export Character to .mcdf", ".mcdf", defaultFileName, ".mcdf", (success, path) =>
            {
                if (!success) return;
                _exportTask = _plugin.CharaDataFileHandler.SaveCharaFileAsync(_exportDescription, path);
            });
        }

        if (!_readExport || !ipcReady)
        {
            ImGui.EndDisabled();
            if (!ipcReady)
            {
                ImGui.TextColored(new Vector4(1, 0, 0, 1), "Glamourer or Penumbra is not available. Please ensure they are installed and running.");
            }
        }

        if (_exportTask != null)
        {
            if (_exportTask.IsCompleted)
            {
                if (_exportTask.IsFaulted)
                {
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), "Export failed. See /xllog for details.");
                    Plugin.Log.Error(_exportTask.Exception, "MCDF Export failed");
                }
                else
                {
                    ImGui.TextColored(new Vector4(0, 1, 0, 1), "Export successful!");
                }
                _exportTask = null;
            }
            else
            {
                ImGui.Text("Exporting...");
            }
        }
    }

    public void Dispose() { }
}
