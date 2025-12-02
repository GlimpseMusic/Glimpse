namespace Glimpse.API.Database;

public interface IMusicDatabase
{
    /// <summary>
    /// The tracks present in the database.
    /// </summary>
    public IReadOnlyDictionary<string, Track> Tracks { get; }

    /// <summary>
    /// The albums present in the database.
    /// </summary>
    public IReadOnlyDictionary<string, Album> Albums { get; }
}