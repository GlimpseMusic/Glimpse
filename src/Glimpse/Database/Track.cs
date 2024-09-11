using Glimpse.Player;

namespace Glimpse.Database;

public struct Track
{
    public uint? TrackNumber;
    
    public string Title;

    public string Artist;

    public string Album;

    public Track(TrackInfo info)
    {
        TrackNumber = info.TrackNumber;
        Title = info.Title;
        Artist = info.Artist;
        Album = info.Album;
    }
}