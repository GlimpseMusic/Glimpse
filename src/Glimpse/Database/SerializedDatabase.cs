using Glimpse.API;
using Glimpse.API.Database;

namespace Glimpse.Database;

public class SerializedDatabase : IConfig
{
    public List<string> Folders;
    public Dictionary<string, Track> Tracks;
    public Dictionary<string, Album> Albums;

    public SerializedDatabase()
    {
        Folders = [];
        Tracks = [];
        Albums = [];
    }

    public SerializedDatabase(List<string> folders, Dictionary<string, Track> tracks, Dictionary<string, Album> albums)
    {
        Folders = folders;
        Tracks = tracks;
        Albums = albums;
    }
}