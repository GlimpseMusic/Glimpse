using System;
using System.Numerics;
using Glimpse.Graphics;
using Hexa.NET.ImGui;

namespace Glimpse.Forms;

public class SettingsPopup : Popup
{
    private Image _glimpseLogo;
    
    public override void Update()
    {
        if (!ImGui.IsPopupOpen("Settings"))
            ImGui.OpenPopup("Settings");
        
        if (ImGui.BeginPopupModal("Settings", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (ImGui.BeginChild("SettingsItems", new Vector2(500, 350)))
            {
                if (ImGui.BeginTabBar("SettingsTab"))
                {
                    if (ImGui.BeginTabItem("Theme"))
                    {
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Player"))
                    {
                        ref bool autoPlay = ref Glimpse.Player.Config.AutoPlay;
                        ref uint sampleRate = ref Glimpse.Player.Config.SampleRate;
                        float speed = (float) Glimpse.Player.Config.SpeedAdjust;

                        ImGui.Checkbox("Auto Play", ref autoPlay);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip("Start playing when a track is selected or added to queue.");
                        
                        ImGui.BeginDisabled();

                        if (ImGui.BeginCombo("Sample Rate", sampleRate.ToString()))
                        {
                            ImGui.EndCombo();
                        }

                        if (ImGui.DragFloat("Speed Adjustment", ref speed, 0.1f))
                            Glimpse.Player.Config.SpeedAdjust = speed;
                        
                        ImGui.EndDisabled();

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("About"))
                    {
                        _glimpseLogo ??= Renderer.CreateImage("Assets/Icons/Glimpse.png");

                        if (ImGui.BeginChild("GlimpseLogo", ImGuiChildFlags.AlwaysAutoResize | ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY))
                        {
                            ImGui.Image((IntPtr) _glimpseLogo.ID, new Vector2(128, 128));
                            ImGui.EndChild();
                        }

                        ImGui.SameLine();
                        
                        if (ImGui.BeginChild("GlimpseText"))
                        {
                            ImGui.Text("Glimpse");
                            ImGui.Text("2024 Aqua Barnes");

                            ImGui.Spacing();
                            ImGui.Text("Code: aquagoose");
                            ImGui.Text("Name + Logo: Nizzine");
                            
                            ImGui.EndChild();
                        }

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();

                    ImGui.EndChild();
                }

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

    public override void Dispose()
    {
        _glimpseLogo?.Dispose();
    }
}