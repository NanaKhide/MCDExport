#define DEBUG

using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.Windowing;
using McdfExporter.Data;
using McdfExporter.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;

namespace McdfExporter.Windows
{
    public class MainWindow : Window, IDisposable
    {
        private readonly Plugin _plugin;
        private readonly CharaDataFileHandler _charaDataFileHandler;
        private readonly McdfApplicationService _mcdfApplier;

        private bool _readExport;
        private string _exportDescription = string.Empty;
        private Task? _exportTask;

        private string _regCharName = string.Empty;
        private string _regCharWorld = string.Empty;
        private string _regCharMcdfPath = "Select MCDF File...";

        private string _debugMcdfPath = string.Empty;
        private Task<Guid?>? _debugApplyTask;

        public MainWindow(Plugin plugin, CharaDataFileHandler charaDataFileHandler, McdfApplicationService mcdfApplier)
            : base("MCDF Exporter", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.MenuBar)
        {
            SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(400, 400), MaximumSize = new Vector2(float.MaxValue, float.MaxValue) };
            _plugin = plugin;
            _charaDataFileHandler = charaDataFileHandler;
            _mcdfApplier = mcdfApplier;
        }

        public override void Draw()
        {
            if (ImGui.BeginTabBar("##mcdfTabs"))
            {
                if (ImGui.BeginTabItem("Exporter")) { DrawExporterTab(); ImGui.EndTabItem(); }
                if (ImGui.BeginTabItem("Registered Characters")) { DrawRegisteredCharactersTab(); ImGui.EndTabItem(); }

#if DEBUG
                if (ImGui.BeginTabItem("Debug Tools")) { DrawDebugTab(); ImGui.EndTabItem(); }
#endif

                ImGui.EndTabBar();
            }
        }

#if DEBUG
        private void DrawDebugTab()
        {
            ImGui.Text("Manual MCDF Application");
            ImGui.Separator();

            var buttonText = string.IsNullOrEmpty(_debugMcdfPath) ? "Select MCDF File..." : Path.GetFileName(_debugMcdfPath);
            if (ImGui.Button(buttonText))
            {
                _plugin.FileDialogManager.OpenFileDialog("Select MCDF File", ".mcdf", (success, path) =>
                {
                    if (success)
                    {
                        _debugMcdfPath = path;
                        _debugApplyTask = null;
                    }
                });
            }

            ImGui.Spacing();

            var currentTarget = Plugin.TargetManager.Target;
            var targetName = currentTarget?.Name.ToString() ?? "None";
            ImGui.Text($"Current Target: {targetName}");

            var isTargetValid = currentTarget is IPlayerCharacter;
            if (!isTargetValid && currentTarget != null)
            {
                ImGui.TextColored(new Vector4(1, 0, 0, 1), "Target must be a player character.");
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            bool canApply = !string.IsNullOrEmpty(_debugMcdfPath) && File.Exists(_debugMcdfPath) && isTargetValid;
            if (!canApply)
            {
                ImGui.BeginDisabled();
            }

            if (ImGui.Button("Apply MCDF to Target"))
            {
                if (currentTarget != null)
                {
                    _debugApplyTask = _mcdfApplier.ApplyMcdf(currentTarget, _debugMcdfPath);
                }
            }

            if (!canApply)
            {
                ImGui.EndDisabled();
            }

            if (_debugApplyTask != null)
            {
                if (_debugApplyTask.IsCompleted)
                {
                    if (_debugApplyTask.IsFaulted)
                    {
                        ImGui.TextColored(new Vector4(1, 0, 0, 1), "Application Failed. See /xllog for details.");
                        Plugin.Log.Error(_debugApplyTask.Exception, "Manual MCDF Apply failed");
                        _debugApplyTask = null;
                    }
                    else if (_debugApplyTask.Result.HasValue)
                    {
                        ImGui.TextColored(new Vector4(0, 1, 0, 1), "Application successful!");
                    }
                    else
                    {
                        ImGui.TextColored(new Vector4(1, 0, 0, 1), "Application failed. MCDF data may be invalid.");
                        _debugApplyTask = null;
                    }
                }
                else
                {
                    ImGui.Text("Applying...");
                }
            }
        }
#endif
        public void Dispose() { }

        private void DrawExporterTab()
        {
            ImGui.Text("Mare Character Data File Export");

            if (ImGui.CollapsingHeader("Help"))
            {
                ImGui.TextWrapped("This feature allows you to pack your character into a .mcdf file and manually send it to other people. Be aware that by sharing this file, you are giving away your current character appearance irrevocably.");
            }

            ImGui.Separator();
            ImGui.Spacing();

            ImGui.Checkbox("##readExport", ref _readExport);
            ImGui.SameLine();
            ImGui.TextWrapped("I understand that by exporting my character data into a file and sending it to other people, I am giving away my current character appearance. People I share my data with have the ability to share it with other people without limitations.");

            ImGui.Spacing();

            var ipcReady = _plugin.IpcManager.Glamourer.ApiAvailable && _plugin.IpcManager.Penumbra.ApiAvailable;
            if (!_readExport || !ipcReady)
                ImGui.BeginDisabled();

            ImGui.InputTextWithHint("##description", "Export Description (optional)", ref _exportDescription, 255);

            if (ImGui.Button("Export Character as MCDF"))
            {
                var defaultFileName = string.IsNullOrEmpty(_exportDescription) ? "export.mcdf" : string.Join("_", $"{_exportDescription}.mcdf".Split(Path.GetInvalidFileNameChars()));
                _plugin.FileDialogManager.SaveFileDialog("Export Character to .mcdf", ".mcdf", defaultFileName, ".mcdf", (success, path) =>
                {
                    if (!success) return;
                    _exportTask = _charaDataFileHandler.SaveCharaFileAsync(_exportDescription, path);
                });
            }

            if (!_readExport || !ipcReady)
                ImGui.EndDisabled();

            if (_exportTask != null)
            {
                if (_exportTask.IsCompleted)
                {
                    ImGui.Text(_exportTask.IsFaulted ? "Export failed. See /xllog for details." : "Export successful!");
                    if (_exportTask.IsFaulted)
                        Plugin.Log.Error(_exportTask.Exception, "MCDF Export failed");
                    _exportTask = null;
                }
                else
                {
                    ImGui.Text("Exporting...");
                }
            }
        }

        private void DrawRegisteredCharactersTab()
        {
            ImGui.InputText("Character Name", ref _regCharName, 100);
            ImGui.InputText("Home World", ref _regCharWorld, 100);
            if (ImGui.Button(_regCharMcdfPath))
            {
                _plugin.FileDialogManager.OpenFileDialog("Select MCDF File", ".mcdf", (success, path) =>
                {
                    if (success) _regCharMcdfPath = path;
                });
            }
            if (ImGui.Button("Register Character"))
            {
                if (!string.IsNullOrEmpty(_regCharName) && !string.IsNullOrEmpty(_regCharWorld) && File.Exists(_regCharMcdfPath))
                {
                    _plugin.RegistrationService.RegisterCharacter(_regCharName, _regCharWorld, _regCharMcdfPath);
                    
                    _regCharName = string.Empty;
                    _regCharWorld = string.Empty;
                    _regCharMcdfPath = "Select MCDF File...";
                }
            }

            ImGui.Separator();

            if (ImGui.BeginTable("##registeredCharactersTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Character");
                ImGui.TableSetupColumn("MCDF File Path");
                ImGui.TableSetupColumn("Actions");
                ImGui.TableHeadersRow();

                foreach (KeyValuePair<string, RegisteredCharacter> entry in _plugin.RegistrationService.RegisteredCharacters)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text($"{entry.Value.Name}@{entry.Value.HomeWorld}");
                    ImGui.TableNextColumn();
                    ImGui.Text(entry.Value.McdfFilePath);
                    ImGui.TableNextColumn();
                    if (ImGui.Button($"Unregister##{entry.Key}"))
                    {
                        _plugin.RegistrationService.UnregisterCharacter(entry.Value.Name, entry.Value.HomeWorld);
                    }
                }
                ImGui.EndTable();
            }
        }
    }
}
