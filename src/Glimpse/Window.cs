using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Glimpse.Forms;
using Glimpse.Graphics;
using Glimpse.Platforms;
using Hexa.NET.ImGui;
using Silk.NET.OpenGL;
using Silk.NET.SDL;
using StbImageSharp;
using PixelFormat = Silk.NET.OpenGL.PixelFormat;
using Renderer = Glimpse.Graphics.Renderer;

namespace Glimpse;

public abstract unsafe class Window : IDisposable
{
    private bool _isCreated;
    private string _title;
    private Size _size;
    
    private Sdl _sdl;
    private Silk.NET.SDL.Window* _window;
    private void* _glContext;

    private List<Popup> _popups;

    public Renderer Renderer;

    public string Title
    {
        get
        {
            if (!_isCreated)
                return _title;

            return _sdl.GetWindowTitleS(_window);
        }
        set
        {
            if (!_isCreated)
                _title = value;
            else
                _sdl.SetWindowTitle(_window, value);
        }
    }

    public Size Size
    {
        get
        {
            if (!_isCreated)
                return _size;

            int w, h;
            _sdl.GetWindowSize(_window, &w, &h);

            return new Size(w, h);
        }
        set
        {
            if (!_isCreated)
                _size = value;
            else
                _sdl.SetWindowSize(_window, value.Width, value.Height);
        }
    }

    protected Window()
    {
        Title = "Window";
        Size = new Size(800, 450);

        _popups = new List<Popup>();
    }

    protected virtual void Initialize() { }

    protected virtual void Update() { }

    public void AddPopup(Popup popup)
    {
        popup.Renderer = Renderer;
        _popups.Add(popup);
    }

    internal uint Create(Sdl sdl, Platform platform)
    {
        _sdl = sdl;

        _sdl.GLSetAttribute(GLattr.ContextMajorVersion, 3);
        _sdl.GLSetAttribute(GLattr.ContextMinorVersion, 3);
        _sdl.GLSetAttribute(GLattr.ContextProfileMask, (int) GLprofile.Core);
        
        const WindowFlags flags = WindowFlags.Opengl | WindowFlags.Resizable | WindowFlags.AllowHighdpi | WindowFlags.Hidden;

        _window = sdl.CreateWindow(_title, Sdl.WindowposCentered, Sdl.WindowposCentered, _size.Width, _size.Height,
            (uint) flags);

        if (_window == null)
            throw new Exception($"Failed to open window: {_sdl.GetErrorS()}");
        
        ImageResult result = ImageResult.FromMemory(File.ReadAllBytes("Assets/Icons/Glimpse.png"));
        Surface* surface;
        fixed (byte* pData = result.Data)
        {
            surface = sdl.CreateRGBSurfaceWithFormatFrom(pData, result.Width, result.Height, 0, result.Width * 4,
                Sdl.PixelformatAbgr8888);
        }

        sdl.SetWindowIcon(_window, surface);

        _glContext = sdl.GLCreateContext(_window);

        sdl.GLMakeCurrent(_window, _glContext);
        Renderer = new Renderer(GL.GetApi(s => (nint) _sdl.GLGetProcAddress(s)), _size);

        _isCreated = true;
        
        Initialize();
        
        _sdl.ShowWindow(_window);
        
        if (OperatingSystem.IsWindows())
        {
            SysWMInfo wmInfo = new SysWMInfo();
            _sdl.GetWindowWMInfo(_window, &wmInfo);
            platform.EnableDarkWindow(wmInfo.Info.Win.Hwnd);
            platform.InitializeMainWindow(wmInfo.Info.Win.Hwnd);
        }

        return _sdl.GetWindowID(_window);
    }

    internal void SetActive()
    {
        _sdl.GLMakeCurrent(_window, _glContext);
    }

    internal void UpdateWindow()
    {
        ImGui.SetCurrentContext(Renderer.ImGui.ImGuiContext);
        ImGui.GetIO().DeltaTime = 1 / 60.0f;
        ImGui.NewFrame();
        
        Renderer.GL.Disable(EnableCap.ScissorTest);
        Update();

        for (int i = 0; i < _popups.Count; i++)
        {
            Popup popup = _popups[i];
            popup.Update();

            if (popup.IsRemoved)
            {
                popup.Dispose();
                _popups.RemoveAt(i);
                i--;
            }
        }
    }

    internal void Present()
    {
        Renderer.ImGui.Render();
        _sdl.GLSetSwapInterval(1);
        _sdl.GLSwapWindow(_window);
    }

    public void Dispose()
    {
        _sdl.DestroyWindow(_window);
    }
}