using Dalamud.Bindings.ImGui;
using McdfExporter.Data;
using McdfExporter.Services;
using System.Collections.Generic;
using System.IO;

namespace McdfExporter.Windows
{
    public class RegisteredCharactersWindow
    {
        private readonly Plugin _plugin;
        private readonly RegistrationService _registrationService;

        private string _regCharName = string.Empty;
        private string _regCharWorld = string.Empty;
        private string _regCharMcdfPath = "Select MCDF File...";

        public RegisteredCharactersWindow(Plugin plugin, RegistrationService registrationService)
        {
            _plugin = plugin;
            _registrationService = registrationService;
        }

        public void Draw()
        {
            if (ImGui.BeginTabItem("MCDF Auto-Application"))
            {
                DrawRegistrationTab();
                ImGui.EndTabItem();
            }
        }

        private void DrawRegistrationTab()
        {
            if (ImGui.CollapsingHeader("Help"))
            {
                ImGui.TextWrapped("This system automatically applies a saved appearance to a character whenever they are visible in your game. To use it, register a character by their full name and home world, then select the corresponding .mcdf file you wish to apply. Please note that this appearance change is only visible to you and requires the .mcdf file to remain at its original location to function correctly.");
                ImGui.Spacing();
                ImGui.TextWrapped("Please allow a few moments for an appearance to be applied when a character appears. For the fastest results, please be patient and avoid toggling the plugin off and on, as this will only slow down the process.");
            }

            ImGui.Separator();
            ImGui.Spacing();

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
                    _registrationService.RegisterCharacter(_regCharName, _regCharWorld, _regCharMcdfPath);
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

                foreach (KeyValuePair<string, RegisteredCharacter> entry in _registrationService.RegisteredCharacters)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text($"{entry.Value.Name}@{entry.Value.HomeWorld}");
                    ImGui.TableNextColumn();
                    ImGui.Text(entry.Value.McdfFilePath);
                    ImGui.TableNextColumn();
                    if (ImGui.Button($"Unregister##{entry.Key}"))
                    {
                        _registrationService.UnregisterCharacter(entry.Value.Name, entry.Value.HomeWorld);
                    }
                }
                ImGui.EndTable();
            }
        }
    }
}
