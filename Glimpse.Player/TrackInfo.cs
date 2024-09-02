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

        string title = file.Tag.Title ?? "Unknown Title";
        string artist = file.Tag.Performers is { Length: > 0 } ? file.Tag.Performers[0] : "Unknown Artist";
        string album = file.Tag.Album ?? "Unknown Album";

        return new TrackInfo(title, artist, album);
    }
}