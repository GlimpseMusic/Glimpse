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

    public void AddDirectory(string directory, AudioPlayer player)
    {
        Logger.Log($"Adding directory {directory} to database");

        foreach (string file in Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
        {
            Logger.Log($"Indexing {file}");
            if (!player.FileIsSupported(file, out _))
                continue;
            
            TrackInfo info = TrackInfo.FromFile(file);
            
            Tracks.Add(file, new Track(info));

            if (info.Album != null)
            {
                if (!Albums.TryGetValue(info.Album, out Album album))
                {
                    album = new Album(info.Album);
                    Albums.Add(info.Album, album);
                }
                
                album.Tracks.Add(file);
            }
        }
    }
}