using Glimpse.API.Database;

namespace Glimpse.Database;

public class MusicDatabase : IMusicDatabase
{
    public const string DatabaseName = "Database/MusicDatabase";

    private readonly Logger _logger;
    private readonly ConfigManager _configManager;
    private readonly SerializedDatabase _database;

    public IReadOnlyList<string> Folders => _database.Folders;

    public IReadOnlyDictionary<string, Track> Tracks => _database.Tracks;

    public IReadOnlyDictionary<string, Album> Albums => _database.Albums;

    public MusicDatabase(Logger logger, ConfigManager configManager)
    {
        _logger = logger;
        _configManager = configManager;

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
}