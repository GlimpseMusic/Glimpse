using TagLib;

namespace Glimpse.Player;

public class TrackInfo
{
    public const string UnknownTitle = "Unknown Title";
    
    public const string UnknownArtist = "Unknown Artist";
    
    public const string UnknownAlbum = "Unknown Album";
    
    public readonly string Title;

    public readonly string Artist;

    public readonly string Album;

    public readonly Image AlbumArt;

    public TrackInfo(string title, string artist, string album, Image albumArt)
    {
        Title = title;
        Artist = artist;
        Album = album;
        AlbumArt = albumArt;
    }

    public static TrackInfo FromFile(string path)
    {
        using File file = File.Create(path);

        string title = file.Tag.Title ?? UnknownTitle;
        string artist = file.Tag.Performers is { Length: > 0 } ? file.Tag.Performers[0] : UnknownArtist;
        string album = file.Tag.Album ?? UnknownAlbum;

        Image albumArt = null;
        
        if (file.Tag.Pictures is { Length: > 0 })
        {
            IPicture picture = file.Tag.Pictures[0];

            albumArt = new Image(picture.Data?.Data, picture.Filename);
        }

        return new TrackInfo(title, artist, album, albumArt);
    }

    public class Image
    {
        public byte[] Data;
        
        public string Location;

        public Image(byte[] data, string location)
        {
            Data = data;
            Location = location;
        }
    }
}