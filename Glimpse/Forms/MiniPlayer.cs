using System.Drawing;
using System.Numerics;
using Glimpse.Player;
using Image = Glimpse.Graphics.Image;

namespace Glimpse.Forms;

public class MiniPlayer : Window
{
    private Image _playButton;
    
    private Image _albumArt;
    
    public MiniPlayer()
    {
        Title = "Glimpse MiniPlayer";
        Size = new Size(250, 250);
    }

    protected override void Initialize()
    {
        _playButton = Renderer.CreateImage("Assets/Icons/PlayButton.png");
        
        AudioPlayer player = Glimpse.Player;

        _albumArt = Renderer.CreateImage(player.TrackInfo.AlbumArt.Data);
    }

    protected override void Update()
    {
        Renderer.Clear(Color.Black);
        
        Renderer.DrawImage(_albumArt, Vector2.Zero, new Size(250, 250), Color.White);
        
        Renderer.DrawRectangle(Color.FromArgb(128, Color.Black), new Vector2(0, 190), new Size(250, 60));
        
        Renderer.DrawImage(_playButton, new Vector2(250 / 2 - 20, 200), new Size(40, 40), Color.White);
    }
}