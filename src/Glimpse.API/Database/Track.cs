using System.Text.Json.Serialization;

namespace Glimpse.API.Database;

public record struct Track
{
    // No need to serialize the path as it's the primary key.
    [JsonIgnore] public string Path;
    
    public uint? TrackNumber;
    
    public string? Title;

    public string? Artist;

    public string? Album;

    public TimeSpan? Length;

    public string? Genre;

    public byte Rating;

    public uint PlayCount;

    public DateTime? LastPlayed;

    public Track(string path, TrackInfo info)
    {
        Path = path;
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