using System.Numerics;
using Glimpse.Locales;
using Hexa.NET.ImGui;

namespace Glimpse.Forms;

public class ManageFoldersPopup : Popup
{
    public override void Update()
    {
        Locale locale = Glimpse.Locale;
        string popupName = locale.GetString("Popup.ManageFolders.Name");
        
        if (!ImGui.IsPopupOpen(popupName))
            ImGui.OpenPopup(popupName);

        bool open = true;
        ImGui.SetNextWindowSize(new Vector2());
        if (ImGui.BeginPopupModal(popupName, ref open))
        {
            ImGui.EndPopup();
        }
        
        if (!open)
            Close();
    }
}