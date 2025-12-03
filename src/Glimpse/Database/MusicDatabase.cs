using Glimpse.API;
using Glimpse.API.Database;
using Glimpse.Audio;
using Track = Glimpse.API.Database.Track;

namespace Glimpse.Database;

public class MusicDatabase : IMusicDatabase, IDisposable
{
    public const string DatabaseName = "Database/MusicDatabase";

    public event IMusicDatabase.OnRefreshAvailable RefreshAvailable;
    
    private readonly Logger _logger;
    private readonly ConfigManager _configManager;
    private readonly AudioPlayer _player;
    private readonly SerializedDatabase _database;
    
    public IReadOnlyList<string> Folders => _database.Folders;

    public IReadOnlyDictionary<string, Track> Tracks => _database.Tracks;

    public IReadOnlyDictionary<string, Album> Albums => _database.Albums;

    public MusicDatabase(Logger logger, ConfigManager configManager, AudioPlayer player)
    {
        _logger = logger;
        _configManager = configManager;
        _player = player;
        RefreshAvailable = delegate { };

        _logger.Log($"Loading database.");
        if (!_configManager.TryGetConfig(DatabaseName, out _database))
        {
            _database = new SerializedDatabase();
            _configManager.WriteConfig(DatabaseName, _database);
        }
    }

    public Track[] SelectAllTracks(OrderBy orderBy = OrderBy.TrackAndAlbum, Direction direction = Direction.Descending)
    {
        // TODO: Implement orderBy and direction
        return _database.Tracks.Values.OrderBy(track => track.Album).ThenBy(track => track.TrackNumber).ToArray();
    }

    public Album[] SelectAllAlbums()
    {
        return _database.Albums.Values.ToArray();
    }

    public Track[] SelectAlbum(string albumName, OrderBy orderBy = OrderBy.TrackNumber, Direction direction = Direction.Descending)
    {
        Album album = _database.Albums[albumName];
        Track[] tracks = new Track[album.Tracks.Count];

        for (int i = 0; i < album.Tracks.Count; i++)
            tracks[i] = _database.Tracks[album.Tracks[i]];

        return tracks.OrderBy(track => track.TrackNumber).ToArray();
    }

    public void UpdateTrack(Track track)
    {
        if (!_database.Tracks.ContainsKey(track.Path))
            throw new Exception($"Cannot update track: Track with path {track.Path} is not present in the database.");
        
        _database.Tracks[track.Path] = track;
    }

    public void UpdateLibrary()
    {
        _logger.Log("Updating library.");
        
        foreach (string dir in _database.Folders)
        {
            _logger.Log($"  Indexing directory \"{dir}\":");
            foreach (string file in Directory.GetFiles(dir))
            {
                _logger.Log($"    Indexing file \"{file}\"");
                TrackInfo info;
                try
                {
                    info = _player.GetTrackInfoForFile(file);
                }
                catch (Exception e)
                {
                    _logger.Log($"    Failed to load file: {e}");
                    continue;
                }

                Track track = new Track(file, info);
                // Populate the new track with the old track's per-user metadata. We don't want to lose that every time
                // we update now do we?
                // ... Do we? I don't actually know. Maybe some people want to hide how many times they've screamed the
                // lyrics to some justin bieber song as loud as possible.
                // .... perhaps that's a bit of an extreme example.
                if (_database.Tracks.TryGetValue(track.Path, out Track oldTrack))
                {
                    track.Rating = oldTrack.Rating;
                    track.PlayCount = oldTrack.PlayCount;
                    track.LastPlayed = oldTrack.LastPlayed;
                }

                _database.Tracks[track.Path] = track;

                string albumName = track.Album ?? string.Empty;
                if (!_database.Albums.TryGetValue(albumName, out Album album))
                    album = new Album(albumName);
                
                album.Tracks.Add(track.Path);
                _database.Albums[albumName] = album;
            }
        }

        RefreshAvailable(this);
    }

    public void Dispose()
    {
        
    }
}