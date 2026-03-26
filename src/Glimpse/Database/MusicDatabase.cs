using Glimpse.API;

namespace Glimpse.Database;

public class MusicDatabase : IConfig
{
    public uint Version;
    
    public Dictionary<string, Track> Tracks;
    public Dictionary<string, Album> Albums;
    public Dictionary<string, Artist> Artists;
    public Dictionary<string, Genre> Genres;

    public MusicDatabase()
    {
        Version = DatabaseManager.DatabaseVersion;
        Tracks = [];
        Albums = [];
        Artists = [];
        Genres = [];
    }
}