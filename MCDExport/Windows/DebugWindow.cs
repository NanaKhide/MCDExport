using Dalamud.Bindings.ImGui;
using McdfExporter.Services;

namespace McdfExporter.Windows
{
    public class DebugWindow
    {
        private readonly AutoApplyService _autoApplyService;

        public DebugWindow(AutoApplyService autoApplyService)
        {
            _autoApplyService = autoApplyService;
        }

        public void Draw()
        {
            if (ImGui.BeginTabItem("Debug"))
            {
                DrawDebugTab();
                ImGui.EndTabItem();
            }
        }

        private void DrawDebugTab()
        {
            if (ImGui.Button("Reapply All Registered Characters"))
            {
                _autoApplyService.ReapplyAllCharacters();
            }
        }
    }
}
