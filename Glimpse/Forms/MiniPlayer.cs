using System.Drawing;
using System.Numerics;
using Glimpse.Player;
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
        TrackInfo info = TrackInfo.FromFile(@"C:\Users\ollie\Music\Copied\01 - April Showers.mp3");
        
        _image = Renderer.CreateImage(info.AlbumArt.Data);
    }

    protected override void Update()
    {
        Renderer.Clear(Color.Black);
        
        Renderer.DrawImage(_image, Vector2.Zero, new Size(250, 250));
    }
}