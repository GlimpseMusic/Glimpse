namespace Glimpse.Api;

public struct TrackInfo
{
    public readonly int? Number;
    
    public readonly string? Title;

    public readonly string? Artist;

    public readonly string? Album;

    public TrackInfo(int? number, string? title, string? artist, string? album)
    {
        Number = number;
        Title = title;
        Artist = artist;
        Album = album;
    }
}