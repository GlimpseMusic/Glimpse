namespace Glimpse.Api;

/// <summary>
/// Represents the various states an <see cref="IAudioPlayer"/> can have.
/// </summary>
public enum PlayerState
{
    /// <summary>
    /// The player is stopped, and there are no tracks in the queue.
    /// </summary>
    Stopped,
    
    /// <summary>
    /// The player is paused.
    /// </summary>
    Paused,
    
    /// <summary>
    /// The player is currently playing a track.
    /// </summary>
    Playing
}