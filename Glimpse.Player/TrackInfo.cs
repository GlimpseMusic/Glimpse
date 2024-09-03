using TagLib;

namespace Glimpse.Player;

public class TrackInfo
{
    public readonly string Title;

    public readonly string Artist;

    public readonly string Album;

    public TrackInfo(string title, string artist, string album)
    {
        Title = title;
        Artist = artist;
        Album = album;
    }

    public static TrackInfo FromFile(string path)
    {
        using File file = File.Create(path);

        string title = file.Tag.Title ?? UnknownTitle;
        string artist = file.Tag.Performers is { Length: > 0 } ? file.Tag.Performers[0] : UnknownArtist;
        string album = file.Tag.Album ?? UnknownAlbum;

        return new TrackInfo(title, artist, album);
    }

    public const string UnknownTitle = "Unknown Title";
    
    public const string UnknownArtist = "Unknown Artist";
    
    public const string UnknownAlbum = "Unknown Album";
}