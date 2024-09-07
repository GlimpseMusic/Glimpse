using System.Collections.Generic;
using System.IO;
using Glimpse.Player;
using Glimpse.Player.Configs;

namespace Glimpse.Database;

public class MusicDatabase : IConfig
{
    public Dictionary<string, Track> Tracks;
    
    public Dictionary<string, Album> Albums;

    public MusicDatabase()
    {
        Tracks = new Dictionary<string, Track>();
        Albums = new Dictionary<string, Album>();
    }

    public void AddDirectory(string directory)
    {
        Logger.Log($"Adding directory {directory} to database");

        foreach (string file in Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
        {
            Logger.Log($"Indexing {file}");
        }
    }
}