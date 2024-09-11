using System.Collections.Generic;

namespace Glimpse.Database;

public struct IndexResult
{
    public string Directory;
    
    public Dictionary<string, Track> Tracks;

    public Dictionary<string, Album> Albums;

    public IndexResult(string directory, Dictionary<string, Track> tracks, Dictionary<string, Album> albums)
    {
        Directory = directory;
        Tracks = tracks;
        Albums = albums;
    }
}