using System.Diagnostics.CodeAnalysis;
using Glimpse.API;
using Glimpse.Audio;

namespace Glimpse.Database;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
public class DatabaseManager : IDatabaseManager
{
    public const string DatabaseName = "Database";
    public const uint DatabaseVersion = 1;
    
    private Logger _logger;
    private ConfigManager _configManager;
    private MusicDatabase _database;
    
    public DatabaseManager(Logger logger, ConfigManager configManager)
    {
        _logger = logger;
        _configManager = configManager;

        if (!_configManager.TryGetConfig(DatabaseName, out _database))
            _database = new MusicDatabase();
    }

    public IEnumerable<(string path, Track track)> GetTracksForAlbum(Album album)
    {
        List<(string, Track)> tracks = new List<(string, Track)>(album.Tracks.Count);
        foreach (string trackPath in album.Tracks)
            tracks.Add((trackPath, _database.Tracks[trackPath]));

        return tracks.OrderBy(tuple => tuple.Item2.TrackNumber).ThenBy(tuple => tuple.Item2.Title);
    }

    /*public void AddIndexToDatabase(in IndexResult index)
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

        foreach ((string name, Artist artist) in index.Artists)
            Artists[name] = artist;
        
        foreach ((string name, Genre genre) in index.Genres)
            Genres[name] = genre;
        
        Refresh();
    }

    public static IndexResult IndexDirectory(string directory, AudioPlayer player, Logger logger, ref string current)
    {
        logger.Log($"Indexing directory {directory}.");

        Dictionary<string, Track> tracks = [];
        Dictionary<string, Album> albums = [];
        Dictionary<string, Artist> artists = [];
        Dictionary<string, Genre> genres = [];

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

            //if (info.Album != null)
            string albumName = info.Album ?? string.Empty;
            if (!albums.TryGetValue(albumName, out Album album))
            {
                album = new Album(albumName);
                albums.Add(albumName, album);
            }
            
            album.Tracks.Add(file.FullName);

            if (info.Artist != null)
            {
                if (!artists.TryGetValue(info.Artist, out Artist artist))
                {
                    artist = new Artist(info.Artist);
                    artists.Add(info.Artist, artist);
                }
                
                artist.Tracks.Add(file.FullName);
            }

            if (info.Genre != null)
            {
                if (!genres.TryGetValue(info.Genre, out Genre genre))
                {
                    genre = new Genre(info.Genre);
                    genres.Add(info.Genre, genre);
                }
                
                genre.Tracks.Add(file.FullName);
            }
        }

        return new IndexResult(directory, tracks, albums, artists, genres);
    }*/
}