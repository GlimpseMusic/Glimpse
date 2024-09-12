using System.Numerics;
using Hexa.NET.ImGui;

namespace Glimpse.Forms;

public class SettingsPopup : Popup
{
    public override void Update()
    {
        if (!ImGui.IsPopupOpen("Settings"))
            ImGui.OpenPopup("Settings");

        ImGui.SetNextWindowSize(new Vector2(500, 400));
        
        if (ImGui.BeginPopupModal("Settings", ImGuiWindowFlags.NoMove))
        {
            if (ImGui.BeginTabBar("SettingsTab"))
            {
                if (ImGui.BeginTabItem("Theme"))
                {
                    ImGui.EndTabItem();
                }
                
                if (ImGui.BeginTabItem("Player"))
                {
                    float speed = (float) Glimpse.Player.Config.SpeedAdjust;

                    if (ImGui.DragFloat("Speed Adjustment", ref speed, 0.1f))
                        Glimpse.Player.Config.SpeedAdjust = speed;
                    
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("About"))
                {
                    ImGui.Text("Glimpse");
                    ImGui.Text("2024 Aqua Barnes");
                    
                    ImGui.Spacing();
                    ImGui.Text("Code: aquagoose");
                    ImGui.Text("Name + Logo: Nizzine");
                    
                    ImGui.EndTabItem();
                }
                
                ImGui.EndTabBar();

                if (ImGui.Button("Save"))
                {
                    
                }
                
                ImGui.SameLine();
                
                if (ImGui.Button("Cancel"))
                    Close();
            }
            
            ImGui.EndPopup();
        }
    }
}