using System;
using System.Collections.Generic;
using Glimpse.Player;
using Hexa.NET.ImGui;
using Silk.NET.SDL;
using Renderer = Glimpse.Graphics.Renderer;

namespace Glimpse;

public static class Glimpse
{
    private static Sdl _sdl;
    private static List<Window> _windows;
    private static Dictionary<uint, Window> _windowIds;

    public static AudioPlayer Player;

    public static void AddWindow(Window window)
    {
        uint id = window.Create(_sdl);
        _windows.Add(window);
        _windowIds.Add(id, window);
    }

    public static unsafe void Run(Window window, string file = null)
    {
        _sdl = Sdl.GetApi();
        
        if (_sdl.Init(Sdl.InitVideo | Sdl.InitEvents) < 0)
            throw new Exception("Failed to initialize SDL.");

        _windows = new List<Window>();
        _windowIds = new Dictionary<uint, Window>();

        /*Player = new AudioPlayer();
        if (file != null)
        {
            Player.ChangeTrack(file);
            //Player.Play();
        }*/
        
        AddWindow(window);

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
                        }

                        break;
                    }

                    case EventType.Mousemotion:
                    {
                        Window wnd = _windowIds[winEvent.Motion.WindowID];
                        ImGui.SetCurrentContext(wnd.ImGuiRenderer.ImGuiContext);
                        
                        ImGui.GetIO().AddMousePosEvent(winEvent.Motion.X, winEvent.Motion.Y);
                        break;
                    }

                    case EventType.Mousebuttondown:
                    {
                        Window wnd = _windowIds[winEvent.Button.WindowID];
                        ImGui.SetCurrentContext(wnd.ImGuiRenderer.ImGuiContext);
                        
                        Console.WriteLine(winEvent.Button.Button);
                        
                        ImGui.GetIO().AddMouseButtonEvent((int) winEvent.Button.Button - 1, true);
                        break;
                    }
                    
                    case EventType.Mousebuttonup:
                    {
                        Window wnd = _windowIds[winEvent.Button.WindowID];
                        ImGui.SetCurrentContext(wnd.ImGuiRenderer.ImGuiContext);
                        
                        ImGui.GetIO().AddMouseButtonEvent((int) winEvent.Button.Button - 1, false);
                        break;
                    }
                }
            }

            foreach (Window wnd in _windows)
            {
                wnd.SetActive();
                wnd.Present();
            }
        }
        
        _sdl.Quit();
        _sdl.Dispose();
    }
}