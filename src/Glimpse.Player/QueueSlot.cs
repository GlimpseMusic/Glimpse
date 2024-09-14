namespace Glimpse.Player;

public enum QueueSlot
{
    /// <summary>
    /// The track will be queued at the end of the current queue.
    /// </summary>
    AtEnd,
    
    /// <summary>
    /// The track will be placed at the end of the user-selected queue.
    /// </summary>
    Queue,
    
    /// <summary>
    /// The track will be inserted into the queue at the end of the current track.
    /// </summary>
    NextTrack,
    
    /// <summary>
    /// The entire queue will be cleared and replaced with the track.
    /// </summary>
    Clear
}