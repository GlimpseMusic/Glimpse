using System.Diagnostics.CodeAnalysis;
using Glimpse.API;
using Glimpse.API.Database;
using Glimpse.Audio;
using Glimpse.Configs;
using Newtonsoft.Json;
using Track = Glimpse.API.Database.Track;

namespace Glimpse.Database;

public class MusicDatabase
{
    private readonly Logger _logger;
    
    
    
    public MusicDatabase()
    {
        Tracks = new Dictionary<string, Track>();
        Albums = new Dictionary<string, Album>();
    }

    public void Refresh()
    {
        Tracks = Tracks.OrderBy(pair => pair.Value.Album).ThenBy(pair => pair.Value.TrackNumber).ToDictionary();
        Albums = Albums.OrderBy(pair => pair.Key).ToDictionary();
    }

    public void AddIndexToDatabase(in IndexResult index)
    {
        _logger.Log($"Adding indexed directory {index.Directory} to dataabase.");

        foreach ((string path, Track track) in index.Tracks)
        {
            Track trk = track;
            
            if (Tracks.TryGetValue(path, out Track oldTrack))
            {
                // Copy over playback metadata to the new track.
                trk.Rating = oldTrack.Rating;
                trk.PlayCount = oldTrack.PlayCount;
                trk.LastPlayed = oldTrack.LastPlayed;
            }
            
            Tracks[path] = trk;
        }

        foreach ((string name, Album album) in index.Albums)
            Albums[name] = album;
        
        Refresh();
    }

    public static IndexResult IndexDirectory(string directory, AudioPlayer player, Logger logger, ref string current)
    {
        logger.Log($"Indexing directory {directory}.");

        Dictionary<string, Track> tracks = new Dictionary<string, Track>();
        Dictionary<string, Album> albums = new Dictionary<string, Album>();

        foreach (FileInfo file in new DirectoryInfo(directory).EnumerateFiles("*.*", SearchOption.AllDirectories).OrderBy(info => info.Name))
        {
            logger.Log($"Indexing {file}");
            current = file.FullName;
            
            TrackInfo info;

            // As GetTrackInfoForFile throws an exception if the track is supported, simply catch all errors, log them,
            // then carry on.
            try
            {
                info = player.GetTrackInfoForFile(file.FullName);
            }
            catch (Exception e)
            {
                logger.Log($"Exception occurred while getting track info: {e}");
                continue;
            }

            tracks.Add(file.FullName, new Track(info));

            if (info.Album != null)
            {
                if (!albums.TryGetValue(info.Album, out Album album))
                {
                    album = new Album(info.Album);
                    albums.Add(info.Album, album);
                }
                
                album.Tracks.Add(file.FullName);
            }
        }

        return new IndexResult(directory, tracks, albums);
    }
}