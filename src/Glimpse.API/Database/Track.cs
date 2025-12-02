namespace Glimpse.API.Database;

public struct Track
{
    public uint? TrackNumber;
    
    public string? Title;

    public string? Artist;

    public string? Album;

    public TimeSpan? Length;

    public string? Genre;

    public byte Rating;

    public byte PlayCount;

    public DateTime? LastPlayed;

    public Track(TrackInfo info)
    {
        TrackNumber = info.TrackNumber;
        Title = info.Title;
        Artist = info.Artist;
        Album = info.Album;
        Length = info.Length;
        Genre = info.Genre;
        Rating = 0;
        PlayCount = 0;
    }
}