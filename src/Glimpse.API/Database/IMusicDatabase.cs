namespace Glimpse.API.Database;

public interface IMusicDatabase
{
    /// <summary>
    /// The list of added folders that are indexed into the database.
    /// </summary>
    public IReadOnlyList<string> Folders { get; }
    
    /// <summary>
    /// The tracks present in the database.
    /// </summary>
    public IReadOnlyDictionary<string, Track> Tracks { get; }

    /// <summary>
    /// The albums present in the database.
    /// </summary>
    public IReadOnlyDictionary<string, Album> Albums { get; }

    /// <summary>
    /// Get <b>all</b> tracks, ordered in the requested way. 
    /// </summary>
    /// <param name="orderBy">The method to order the tracks by.</param>
    /// <param name="direction">The direction to order the tracks.</param>
    /// <returns>An ordered array of tracks.</returns>
    public Track[] SelectAllTracks(OrderBy orderBy = OrderBy.TrackAndAlbum, Direction direction = Direction.Descending);
    
    /// <summary>
    /// Get <b>all</b> albums.
    /// </summary>
    /// <returns>An array of albums.</returns>
    public Album[] SelectAllAlbums();
    
    /// <summary>
    /// Get all tracks in an album, ordered in the requested way.
    /// </summary>
    /// <param name="albumName">The album name.</param>
    /// <param name="orderBy">The method to order the tracks by.</param>
    /// <param name="direction">The direction to order the tracks.</param>
    /// <returns>An ordered array of tracks.</returns>
    /// <remarks><see cref="OrderBy.TrackAndAlbum"/> and <see cref="OrderBy.TrackNumber"/> will have the same effect.</remarks>
    public Track[] SelectAlbum(string albumName, OrderBy orderBy = OrderBy.TrackNumber, Direction direction = Direction.Descending);
}