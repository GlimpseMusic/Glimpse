using System;
using Silk.NET.OpenGL;
using Silk.NET.SDL;
using Renderer = Glimpse.Graphics.Renderer;

namespace Glimpse;

public abstract unsafe class Window : IDisposable
{
    private Sdl _sdl;
    private Silk.NET.SDL.Window* _window;
    private void* _glContext;

    public Renderer Renderer;

    protected Window(string title)
    {
        
    }

    protected virtual void Initialize() { }

    protected virtual void Update() { }

    internal uint Create(Sdl sdl)
    {
        _sdl = sdl;

        _sdl.GLSetAttribute(GLattr.ContextMajorVersion, 3);
        _sdl.GLSetAttribute(GLattr.ContextMinorVersion, 3);
        _sdl.GLSetAttribute(GLattr.ContextProfileMask, (int) GLprofile.Core);
        
        const WindowFlags flags = WindowFlags.Opengl | WindowFlags.Resizable | WindowFlags.AllowHighdpi;

        _window = sdl.CreateWindow("Window", Sdl.WindowposCentered, Sdl.WindowposCentered, 1280, 720, (uint) flags);

        if (_window == null)
            throw new Exception($"Failed to open window: {_sdl.GetErrorS()}");

        _glContext = sdl.GLCreateContext(_window);

        sdl.GLMakeCurrent(_window, _glContext);
        Renderer = new Renderer(GL.GetApi(s => (nint) _sdl.GLGetProcAddress(s)));
        
        Initialize();

        return _sdl.GetWindowID(_window);
    }

    internal void SetActive()
    {
        _sdl.GLMakeCurrent(_window, _glContext);
        Update();
    }

    internal void Present()
    {
        _sdl.GLSetSwapInterval(1);
        _sdl.GLSwapWindow(_window);
    }

    public void Dispose()
    {
        _sdl.DestroyWindow(_window);
    }
}