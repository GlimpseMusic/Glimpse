using Hexa.NET.ImGui;

namespace Glimpse.Forms;

public class StyleEditorPopup : Popup
{
    public override void Update()
    {
        ImGui.ShowStyleEditor();
    }
}