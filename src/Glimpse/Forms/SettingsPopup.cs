using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Numerics;
using System.Text.Json;
using Glimpse.API;
using Glimpse.API.UI;
using Glimpse.Assets;
using Glimpse.Configs;
using Glimpse.Forms.Widgets;
using Hexa.NET.ImGui;
using piko.SDL3;
using Image = Glimpse.Graphics.Image;

namespace Glimpse.Forms;

public class SettingsPopup : Popup
{
    private ImmediateGUI _gui;

    private GlimpseConfig _currentConfig;
    private ThemeWidget _themeWidget;
    
    private Image? _glimpseLogo;
    private string? _currentPlugin;

    private Image? _transportDown;
    private Image? _transportUp;

    public override void Open()
    {
        _gui = new ImmediateGUI();

        _currentConfig = Glimpse.Config;
        _currentConfig.Plugins.EnabledPlugins = new HashSet<string>(Glimpse.Config.Plugins.EnabledPlugins);

        _themeWidget = new ThemeWidget(this);
    }

    protected override void Update(float dt)
    {
        _gui.Scale = Scale;

        Locale currentLocale = Glimpse.Locale;

        ImGuiWindowFlags flags = ImGuiWindowFlags.None;
        if (Glimpse.Config != _currentConfig)
            flags |= ImGuiWindowFlags.UnsavedDocument;
        
        if (ImGui.OpenPopupModal(currentLocale.GetString("Popup.Settings.Name"), ScaleVec(620, 570), flags))
        {
            ImGui.BeginChild("SettingsItems", ScaleVec(600, 500));
            {
                if (ImGui.BeginTabBar("SettingsTab"))
                {
                    if (ImGui.BeginTabItem(currentLocale.GetString("Popup.Settings.Tab.General")))
                    {
                        if (ImGui.BeginCombo(currentLocale.GetString("Popup.Settings.Tab.General.Language"),
                                currentLocale.DisplayName))
                        {
                            foreach ((string id, Locale.AvailableLocale locale) in Locale.AvailableLocales.Locales)
                            {
                                if (ImGui.Selectable(locale.DisplayName, currentLocale.ID == id ? ImGuiSelectableFlags.Highlight : ImGuiSelectableFlags.None))
                                {
                                    _currentConfig.General.Language = id;
                                    Glimpse.Locale = Locale.LoadLocale(id);
                                }
                            }

                            ImGui.EndCombo();
                        }

                        ImGui.Checkbox(currentLocale.GetString("Popup.Settings.Tab.General.EnableDeleteFile"),
                            ref _currentConfig.General.EnableFileDeletion);
                        ImGui.SetItemTooltipUnformatted(
                            currentLocale.GetString("Popup.Settings.Tab.General.EnableDeleteFile.Tooltip"));

#if !DISABLE_AUTOUPDATE
                        ImGui.Checkbox(currentLocale.GetString("Popup.Settings.Tab.General.CheckForUpdates"),
                            ref _currentConfig.General.EnableUpdateChecking);
                        ImGui.SetItemTooltipUnformatted(currentLocale.GetString("Popup.Settings.Tab.General.CheckForUpdates.Tooltip"));
#endif

                        ImGui.EndTabItem();
                    }
                    
                    if (ImGui.BeginTabItem(currentLocale.GetString("Popup.Settings.Tab.Appearance")))
                    {
                        ImGui.SeparatorText(currentLocale.GetString("Popup.Settings.Tab.Appearance.Theme"));

                        _themeWidget.Update(ref _currentConfig);

                        if (ImGui.Button(currentLocale.GetString("Popup.Settings.Tab.Appearance.OpenThemeEditor")))
                        {
                            Close();
                            Glimpse.MainWindow.AddPopup(new ThemeEditor());
                        }

                        ImGui.SeparatorText(currentLocale.GetString("Popup.Settings.Tab.Appearance.TransportLocation"));

                        _transportDown ??= Renderer.CreateImage("asset://Images.TransportDown.png");
                        _transportUp ??= Renderer.CreateImage("asset://Images.TransportUp.png");
                        
                        string up = currentLocale.GetString("Popup.Settings.Tab.Appearance.TransportLocation.Up");
                        string down = currentLocale.GetString("Popup.Settings.Tab.Appearance.TransportLocation.Down");
                        
                        if (ImGui.SelectButton("TransportDown", _transportDown,
                            ScaleVec(_transportDown.Width * 0.25f, _transportDown.Height * 0.25f),
                            !_currentConfig.Appearance.SwapTransportControls))
                        {
                            _currentConfig.Appearance.SwapTransportControls = false;
                        }
                        ImGui.SetItemTooltipUnformatted(down);

                        ImGui.SameLine();
                        
                        if (ImGui.SelectButton("TransportUp", _transportUp,
                                ScaleVec(_transportUp.Width * 0.25f, _transportUp.Height * 0.25f),
                                _currentConfig.Appearance.SwapTransportControls))
                        {
                            _currentConfig.Appearance.SwapTransportControls = true;
                        }
                        ImGui.SetItemTooltipUnformatted(up);
                        
                        ImGui.SeparatorText(currentLocale.GetString("Popup.Settings.Tab.Appearance.Misc"));

                        ImGui.Checkbox(currentLocale.GetString("Popup.Settings.Tab.Appearance.ConfineAlbumArtToSquare"),
                            ref _currentConfig.Appearance.ConfineAlbumArtToSquare);
                        ImGui.SetItemTooltipUnformatted(currentLocale.GetString("Popup.Settings.Tab.Appearance.ConfineAlbumArtToSquare.Tooltip"));
                        
                        ImGui.EndTabItem();
                    }

                    /*if (ImGui.BeginTabItem(locale.GetString("Popup.Settings.Tab.Player")))
                    {
                        ref float volume = ref _currentConfig.Volume;
                        ref uint sampleRate = ref _currentConfig.SampleRate;
                        //float speed = (float) _currentConfig.SpeedAdjust;

                        ImGui.SeparatorText(locale.GetString("Popup.Settings.Tab.Player.PlaybackHeading"));

                        if (ImGui.SliderFloat(locale.GetString("Popup.Settings.Tab.Player.Volume"), ref volume, 0, 1, "%.3f"))
                            Glimpse.Player.Volume = volume;
                        
                        /*ImGui.Checkbox("Auto Play", ref autoPlay);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip("Start playing when a track is selected or added to queue.");
                        
                        //if (ImGui.DragFloat("Speed Adjustment", ref speed, 0.01f, 0.01f, 10))
                        //    _currentConfig.SpeedAdjust = speed;
                        
                        ImGui.SeparatorText(locale.GetString("Popup.Settings.Tab.Player.DeviceHeading"));
                        ImGui.BeginDisabled();
                        if (ImGui.BeginCombo(locale.GetString("SampleRate"), sampleRate.ToString()))
                        {
                            ImGui.EndCombo();
                        }
                        ImGui.EndDisabled();

                        ImGui.EndTabItem();
                    }*/
#if !PUBLISH_AOT
                    if (ImGui.BeginTabItem(currentLocale.GetString("Popup.Settings.Tab.Plugins")))
                    {
                        if (Glimpse.Plugins == null || Glimpse.Plugins.Count == 0)
                        {
                            ImGui.TextUnformatted(currentLocale.GetString("Popup.Settings.Tab.Plugins.NoneAvailable"));
                        }
                        else
                        {
                            foreach ((string name, IPlugin plugin) in Glimpse.Plugins)
                            {
                                ImGui.BeginChild("PluginsList", new Vector2(150, 0));
                                {
                                    if (ImGui.Selectable(plugin.Name, name == _currentPlugin))
                                        _currentPlugin = name;

                                    ImGui.EndChild();
                                }

                                ImGui.SameLine();

                                ImGui.BeginChild("PluginSettings");
                                {
                                    if (name == _currentPlugin)
                                    {
                                        bool enabled = _currentConfig.Plugins.EnabledPlugins.Contains(_currentPlugin);
                                        if (ImGui.Checkbox(currentLocale.GetString("Checkbox.Enabled"), ref enabled))
                                        {
                                            if (enabled)
                                                _currentConfig.Plugins.EnabledPlugins.Add(_currentPlugin);
                                            else
                                                _currentConfig.Plugins.EnabledPlugins.Remove(_currentPlugin);
                                        }
                                        
                                        // TODO: Hack - ideally would display the GUI for all plugins even if disabled
                                        //       Need some sort of API to ensure the plugins always know the config is
                                        //       valid before displaying the GUI?
                                        if (enabled)
                                        {
                                            ImGui.Separator();
                                            if (Glimpse.Plugins[_currentPlugin].IsInitialized)
                                                Glimpse.Plugins[_currentPlugin].OnGUI(_gui);
                                        }
                                    }

                                    ImGui.EndChild();
                                }
                            }
                        }

                        ImGui.EndTabItem();
                    }
#endif

                    if (ImGui.BeginTabItem(currentLocale.GetString("Popup.Settings.Tab.About")))
                    {
                        _glimpseLogo ??= Renderer.CreateImage("asset://Icons.Glimpse.png");

                        ImGui.BeginChild("GlimpseLogo", ImGuiChildFlags.AlwaysAutoResize | ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY);
                        {
                            ImGui.Image(_glimpseLogo, ScaleVec(128, 128));
                            ImGui.EndChild();
                        }

                        ImGui.SameLine();

                        ImGui.BeginChild("GlimpseText", ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY);
                        {
                            ImGui.PushFont(null, 32);
                            ImGui.TextUnformatted(currentLocale.GetString("Popup.Settings.Tab.About.AppName", Glimpse.Version));
                            ImGui.PopFont();
                            ImGui.TextUnformatted("2026 aquagoose");

                            ImGui.Spacing();
                            ImGui.TextUnformatted(currentLocale.GetString("Popup.Settings.Tab.About.Credits"));
                            
                            ImGui.Spacing();
                            ImGui.TextLink(currentLocale.GetString("Popup.Settings.Tab.About.Website"),
                                "https://glimpseplayer.com");
                            
                            ImGui.SameLine();
                            
                            ImGui.TextLink(currentLocale.GetString("Popup.Settings.Tab.About.Donate"),
                                "https://glimpseplayer.com/donate");
                            
                            ImGui.SameLine();
                            
                            ImGui.TextLink(currentLocale.GetString("Popup.Settings.Tab.About.Repository"),
                                "https://glimpseplayer.com/repo");
                            
                            ImGui.SameLine();
                            
                            ImGui.TextLink(currentLocale.GetString("Popup.Settings.Tab.About.Discord"),
                                "https://glimpseplayer.com/discord");
                            
                            ImGui.EndChild();
                        }

                        ImGui.SeparatorText(currentLocale.GetString("Popup.Settings.Tab.About.OpenSourceLibraries"));
                        {
                            ImGui.BeginChild("OSLibraries");
                            {
                                ImGui.TextLink("Slant", "https://github.com/aquagoose/Slant");
                                ImGui.TextLink("Hexa.NET.ImGui", "https://github.com/HexaEngine/Hexa.NET.ImGui");
                                ImGui.TextLink("Silk.NET", "https://dotnet.github.io/Silk.NET/");
                                ImGui.TextLink("piko", "https://github.com/aquagoose/piko");
                                ImGui.TextLink("TagLibSharp", "https://github.com/mono/taglib-sharp");
                                ImGui.TextLink("ImageSharp", "https://sixlabors.com/products/imagesharp/");
                                ImGui.TextLink("empress", "https://github.com/aquagoose/empress");
                                ImGui.TextLink("DiscordRichPresence", "https://github.com/Lachee/discord-rpc-csharp");
                                ImGui.TextLink("MetaBrainz.MusicBrainz", "https://github.com/Zastai/MetaBrainz.MusicBrainz");
                                ImGui.TextLink("MetaBrainz.MusicBrainz.CoverArt", "https://github.com/Zastai/MetaBrainz.MusicBrainz.CoverArt");
                                ImGui.TextLink("TerraFX.Interop.Windows", "https://github.com/terrafx/terrafx.interop.windows");

                                ImGui.EndChild();
                            }
                        }

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();

                    ImGui.EndChild();
                }

                if (ImGui.Button(currentLocale.GetString("Button.Save")))
                {
                    Apply();
                    Close();
                }
                
                ImGui.SameLine();
                
                if (ImGui.Button(currentLocale.GetString("Button.Cancel")))
                    Close();
            }
            
            ImGui.EndPopup();
        }
    }

    private void Apply()
    {
        GlimpseConfig oldConfig = Glimpse.Config;
        if (oldConfig == _currentConfig)
            return;

        Logger logger = Glimpse.Logger;
        
        logger.Log("Saving and applying config changes.");
        //Glimpse.Player.Stop();
        
        Glimpse.Config = _currentConfig;
        Glimpse.ConfigManager.WriteConfig(GlimpseConfig.ConfigName, Glimpse.Config);
        
        if (_currentConfig.Appearance.SwapTransportControls != oldConfig.Appearance.SwapTransportControls || _currentConfig.Appearance.Theme != oldConfig.Appearance.Theme || _currentConfig.Appearance.PreferredColorScheme != oldConfig.Appearance.PreferredColorScheme)
            ((GlimpsePlayer) Glimpse.MainWindow).RefreshLayout();

        if (Glimpse.Plugins == null)
            return;
        
        foreach ((string name, IPlugin plugin) in Glimpse.Plugins)
        {
            // Plugin has been disabled
            if (oldConfig.Plugins.EnabledPlugins.Contains(name) && !_currentConfig.Plugins.EnabledPlugins.Contains(name))
            {
                logger.Log($"Disabling plugin {name}");
                plugin.Dispose();
            }
            // Plugin has been enabled
            else if (_currentConfig.Plugins.EnabledPlugins.Contains(name) && !oldConfig.Plugins.EnabledPlugins.Contains(name))
            {
                logger.Log($"Enabling plugin {name}");
                plugin.Initialize(Glimpse);
            }
        }
    }

    public override void Dispose()
    {
        _glimpseLogo?.Dispose();
    }

    private class ImmediateGUI : IImmediateGUI
    {
        public float Scale;

        public ImmediateGUI()
        {
            Scale = 1;
        }

        public void Separator()
        {
            ImGui.Separator();
        }

        public void Separator(string heading)
        {
            ImGui.SeparatorText(heading);
        }

        public void Text(string text)
        {
            ImGui.TextUnformatted(text);
        }

        public void Text(string text, uint size)
        {
            ImGui.PushFont(ImFontPtr.Null, size * Scale);
            ImGui.TextUnformatted(text);
            ImGui.PopFont();
        }

        public bool Button(string text)
        {
            return ImGui.Button(text);
        }

        public bool Button(string text, Size size)
        {
            return ImGui.Button(text, new Vector2(size.Width * Scale, size.Height * Scale));
        }

        public bool Checkbox(string text, ref bool ticked)
        {
            return ImGui.Checkbox(text, ref ticked);
        }

        public bool Dropdown(string label, ref int value, params ReadOnlySpan<string> items)
        {
            bool hasItemBeenSelected = false;

            if (ImGui.BeginCombo(label, items[value]))
            {
                for (int i = 0; i < items.Length; i++)
                {
                    if (ImGui.Selectable(items[i]))
                    {
                        hasItemBeenSelected = true;
                        value = i;
                    }
                }

                ImGui.EndCombo();
            }

            return hasItemBeenSelected;
        }
    }
}