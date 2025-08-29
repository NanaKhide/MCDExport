using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using McdfExporter.Services;
using System;
using System.Numerics;

namespace McdfExporter.Windows
{
    public class MainWindow : Window, IDisposable
    {
        private readonly ExportWindow _exportWindow;
        private readonly RegisteredCharactersWindow _registeredCharactersWindow;

        public MainWindow(Plugin plugin, CharaDataFileHandler charaDataFileHandler, RegistrationService registrationService)
            : base("MCDF Exporter", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(450, 300),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };

            _exportWindow = new ExportWindow(plugin, charaDataFileHandler);
            _registeredCharactersWindow = new RegisteredCharactersWindow(plugin, registrationService);
        }

        public override void Draw()
        {
            if (ImGui.BeginTabBar("mcdfTabs", ImGuiTabBarFlags.None))
            {
                _exportWindow.Draw();
                _registeredCharactersWindow.Draw();

                ImGui.EndTabBar();
            }
        }

        public void Dispose() { }
    }
}
