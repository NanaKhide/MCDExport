using Dalamud.Bindings.ImGui;
using McdfExporter.Data;
using McdfExporter.Services;
using System.Diagnostics;
using System.IO;
using System.Numerics;

namespace McdfExporter.Windows
{
    public class ExportWindow
    {
        private readonly Plugin _plugin;
        private readonly CharaDataFileHandler _charaDataFileHandler;

        private bool _readExport;
        private string _exportDescription = string.Empty;
        private ExportProgress? _currentExportProgress;
        private readonly Stopwatch _resultDisplayTimer = new();

        public ExportWindow(Plugin plugin, CharaDataFileHandler charaDataFileHandler)
        {
            _plugin = plugin;
            _charaDataFileHandler = charaDataFileHandler;
        }

        public void Draw()
        {
            if (ImGui.BeginTabItem("Exporter"))
            {
                DrawExporterTab();
                ImGui.EndTabItem();
            }
        }

        private void DrawExporterTab()
        {
            ImGui.Spacing();
            ImGui.Checkbox("##readExport", ref _readExport);
            ImGui.SameLine();
            ImGui.TextWrapped("I understand that by exporting my character data into a file and sending it to other people, I am giving away my current character appearance. People I share my data with have the ability to share it with other people without limitations.");
            ImGui.Spacing();

            var ipcReady = _plugin.IpcManager.Glamourer.ApiAvailable && _plugin.IpcManager.Penumbra.ApiAvailable;
            bool canExport = _readExport && ipcReady;
            if (!canExport) ImGui.BeginDisabled();

            ImGui.InputTextWithHint("##description", "Export Description (optional)", ref _exportDescription, 255);

            if (!canExport) ImGui.EndDisabled();

            bool isProcessing = _currentExportProgress != null && !_currentExportProgress.IsFinished;

            if (!canExport || isProcessing)
            {
                ImGui.BeginDisabled();
            }

            if (ImGui.Button("Export Character as MCDF"))
            {
                var defaultFileName = string.IsNullOrEmpty(_exportDescription) ? "export.mcdf" : string.Join("_", $"{_exportDescription}.mcdf".Split(Path.GetInvalidFileNameChars()));
                _plugin.FileDialogManager.SaveFileDialog("Export Character to .mcdf", ".mcdf", defaultFileName, ".mcdf", (success, path) =>
                {
                    if (!success) return;
                    _currentExportProgress = new ExportProgress();
                    _ = _charaDataFileHandler.SaveCharaFileAsync(_exportDescription, path, _currentExportProgress);
                });
            }

            if (!canExport || isProcessing)
            {
                ImGui.EndDisabled();
            }

            if (_currentExportProgress != null)
            {
                ImGui.Spacing();

                if (!isProcessing)
                {
                    if (!_resultDisplayTimer.IsRunning)
                    {
                        _resultDisplayTimer.Restart();
                    }

                    if (_currentExportProgress.IsError)
                    {
                        ImGui.TextColored(new Vector4(1.0f, 0.3f, 0.3f, 1.0f), "Export failed! See the /xllog for details.");
                    }
                    else
                    {
                        ImGui.TextColored(new Vector4(0.3f, 1.0f, 0.3f, 1.0f), "Export successful!");
                    }
                    if (_resultDisplayTimer.ElapsedMilliseconds > 5000)
                    {
                        _currentExportProgress = null;
                        _resultDisplayTimer.Stop();
                    }
                }
                else
                {
                    ImGui.Text(_currentExportProgress.Message);
                    ImGui.ProgressBar(_currentExportProgress.ProgressFraction, new Vector2(-1, 0), $"{_currentExportProgress.FilesProcessed} / {_currentExportProgress.TotalFiles}");
                    ImGui.TextWrapped("Please wait, the game may be unresponsive during this process.");
                }
            }
        }
    }
}
