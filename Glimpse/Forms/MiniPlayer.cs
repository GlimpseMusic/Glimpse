using Silk.NET.OpenGL;

namespace Glimpse.Forms;

public class MiniPlayer : Window
{
    public MiniPlayer() : base("Glimpse MiniPlayer") { }

    protected override void Initialize()
    {
        Glimpse.AddWindow(new TestWindow());
    }

    protected override void Update()
    {
        Renderer.GL.ClearColor(1.0f, 0.5f, 0.25f, 1.0f);
        Renderer.GL.Clear(ClearBufferMask.ColorBufferBit);
    }
}