using System.Drawing;
using System.Numerics;
using Glimpse.Player;
using Hexa.NET.ImGui;
using Image = Glimpse.Graphics.Image;

namespace Glimpse.Forms;

public class GlimpsePlayer : Window
{
    public GlimpsePlayer()
    {
        Title = "Glimpse";
        Size = new Size(800, 450);
    }

    protected override void Update()
    {
        Renderer.Clear(Color.Black);
        
        ImGui.ShowDemoWindow();
    }
}