using System.Drawing;
using Silk.NET.OpenGL;

namespace Glimpse.Forms;

public class MiniPlayer : Window
{
    public MiniPlayer(string[] args)
    {
        Title = "Glimpse MiniPlayer";
        Size = new Size(250, 250);
    }

    protected override void Initialize()
    {
        
    }

    protected override void Update()
    {
        Renderer.Clear(Color.Black);
    }
}