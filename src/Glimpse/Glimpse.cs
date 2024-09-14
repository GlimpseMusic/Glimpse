using System;
using System.Collections.Generic;
using System.Drawing;
using Glimpse.Database;
using Glimpse.Platforms;
using Glimpse.Player;
using Glimpse.Player.Configs;
using Hexa.NET.ImGui;
using Silk.NET.SDL;
using Renderer = Glimpse.Graphics.Renderer;

namespace Glimpse;

public static class Glimpse
{
    private static Sdl _sdl;
    private static List<Window> _windows;
    private static Dictionary<uint, Window> _windowIds;

    public static Platform Platform;

    public static AudioPlayer Player;

    public static MusicDatabase Database;

    public static void AddWindow(Window window)
    {
        uint id = window.Create(_sdl, Platform);
        _windows.Add(window);
        _windowIds.Add(id, window);
    }

    public static unsafe void Run(Window window, string[] args)
    {
        Platform = Platform.AutoDetect();
        Logger.Log($"Detected platform {Platform.GetType()}");
        
        Platform.EnableDPIAwareness();
        
        _sdl = Sdl.GetApi();
        
        if (_sdl.Init(Sdl.InitVideo | Sdl.InitEvents) < 0)
            throw new Exception("Failed to initialize SDL.");

        _windows = new List<Window>();
        _windowIds = new Dictionary<uint, Window>();

        Player = new AudioPlayer();

        if (!IConfig.TryGetConfig("Database/MusicDatabase", out Database))
        {
            Database = new MusicDatabase();
            IConfig.WriteConfig("Database/MusicDatabase", Database);
        }
        
        AddWindow(window);
        
        if (args.Length > 0)
        {
            Player.QueueTrack(args[0], QueueSlot.AtEnd);
            Player.Play();
        }

        while (_windows.Count > 0)
        {
            Event winEvent;
            while (_sdl.PollEvent(&winEvent) != 0)
            {
                switch ((EventType) winEvent.Type)
                {
                    case EventType.Windowevent:
                    {
                        switch ((WindowEventID) winEvent.Window.Event)
                        {
                            case WindowEventID.Close:
                            {
                                Window wnd = _windowIds[winEvent.Window.WindowID];
                                wnd.Dispose();
                                _windowIds.Remove(winEvent.Window.WindowID);
                                _windows.Remove(wnd);
                                break;
                            }

                            case WindowEventID.Resized:
                            {
                                Window wnd = _windowIds[winEvent.Window.WindowID];
                                Size newSize = new Size(winEvent.Window.Data1, winEvent.Window.Data2);
                                wnd.SetActive();
                                wnd.Renderer.Resize(newSize);
                                
                                break;
                            }
                        }

                        break;
                    }

                    case EventType.Mousemotion:
                    {
                        Window wnd = _windowIds[winEvent.Motion.WindowID];
                        ImGui.SetCurrentContext(wnd.Renderer.ImGui.ImGuiContext);
                        
                        ImGui.GetIO().AddMousePosEvent(winEvent.Motion.X, winEvent.Motion.Y);
                        break;
                    }

                    case EventType.Mousebuttondown:
                    {
                        Window wnd = _windowIds[winEvent.Button.WindowID];
                        ImGui.SetCurrentContext(wnd.Renderer.ImGui.ImGuiContext);
                        
                        ImGui.GetIO().AddMouseButtonEvent((int) SdlButtonToImGui(winEvent.Button.Button), true);
                        break;
                    }
                    
                    case EventType.Mousebuttonup:
                    {
                        Window wnd = _windowIds[winEvent.Button.WindowID];
                        ImGui.SetCurrentContext(wnd.Renderer.ImGui.ImGuiContext);
                        
                        ImGui.GetIO().AddMouseButtonEvent((int) SdlButtonToImGui(winEvent.Button.Button), false);
                        break;
                    }

                    case EventType.Mousewheel:
                    {
                        Window wnd = _windowIds[winEvent.Button.WindowID];
                        ImGui.SetCurrentContext(wnd.Renderer.ImGui.ImGuiContext);
                        
                        ImGui.GetIO().AddMouseWheelEvent(winEvent.Wheel.X, winEvent.Wheel.Y);
                        break;
                    }
                }
            }

            foreach (Window wnd in _windows)
            {
                wnd.SetActive();
                wnd.UpdateWindow();
                wnd.Present();
            }
        }
        
        Player.Dispose();
        
        _sdl.Quit();
        _sdl.Dispose();
    }

    private static ImGuiMouseButton SdlButtonToImGui(uint button)
    {
        return button switch
        {
            Sdl.ButtonLeft => ImGuiMouseButton.Left,
            Sdl.ButtonRight => ImGuiMouseButton.Right,
            Sdl.ButtonMiddle => ImGuiMouseButton.Middle,
            _ => ImGuiMouseButton.Count
        };
    }
}