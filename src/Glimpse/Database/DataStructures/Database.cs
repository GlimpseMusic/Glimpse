using Glimpse.API;
using Glimpse.API.Database;

namespace Glimpse.Database.DataStructures;

public class Database : IConfig
{
    public const string DatabaseName = "Database/MusicDatabase";
    
    public Dictionary<string, Track> Tracks;
    public Dictionary<string, Album> Albums;

    public Database()
    {
        Tracks = [];
        Albums = [];
    }

    public Database(Dictionary<string, Track> tracks, Dictionary<string, Album> albums)
    {
        Tracks = tracks;
        Albums = albums;
    }
}