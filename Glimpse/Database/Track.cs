using Glimpse.Player;

namespace Glimpse.Database;

public struct Track
{
    public string Title;

    public string Artist;

    public string Album;

    public Track(TrackInfo info)
    {
        Title = info.Title;
        Artist = info.Artist;
        Album = info.Album;
    }
}