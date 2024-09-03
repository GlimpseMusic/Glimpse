using System.Drawing;
using System.Numerics;
using Image = Glimpse.Graphics.Image;

namespace Glimpse.Forms;

public class MiniPlayer : Window
{
    private Image _image;
    
    public MiniPlayer(string[] args)
    {
        Title = "Glimpse MiniPlayer";
        Size = new Size(250, 250);
    }

    protected override void Initialize()
    {
        _image = Renderer.CreateImage([255, 255, 255, 255], 1, 1);
    }

    protected override void Update()
    {
        Renderer.Clear(Color.Black);
        
        Renderer.DrawImage(_image, Vector2.Zero);
    }
}