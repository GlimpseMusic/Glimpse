using Silk.NET.OpenGL;

namespace Glimpse.Forms;

public class TestWindow : Window
{
    public TestWindow() : base("Test") { }

    protected override void Update()
    {
        Renderer.GL.ClearColor(0.25f, 0.5f, 1.0f, 1.0f);
        Renderer.GL.Clear(ClearBufferMask.ColorBufferBit);
    }
}