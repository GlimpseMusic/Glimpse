namespace Glimpse.Api;

public interface IAudioPlayer
{
    /// <summary>
    /// Invoked whenever the track changes - either automatically, or through manual input.
    /// </summary>
    public event OnTrackChanged TrackChanged;

    /// <summary>
    /// Invoked whenever the player's state changes. Note that this is also called when a <see cref="Seek"/> is performed.
    /// </summary>
    public event OnStateChanged StateChanged;
    
    /// <summary>
    /// Get the current track's <see cref="TrackInfo"/>, which contains information such as its name and track number.
    /// </summary>
    public TrackInfo CurrentTrack { get; }
    
    /// <summary>
    /// Get the current track's index in the queue.
    /// </summary>
    public int CurrentTrackIndex { get; }
    
    /// <summary>
    /// The current player state.
    /// </summary>
    public TrackState State { get; }
    
    /// <summary>
    /// The number of seconds elapsed since the track started.
    /// </summary>
    public int ElapsedSeconds { get; }
    
    /// <summary>
    /// The calculated length of the track in seconds.
    /// </summary>
    public int TrackLength { get; }
    
    /// <summary>
    /// Get an enumerator of all the currently queued tracks.
    /// </summary>
    public IEnumerable<string> QueuedTracks { get; }
    
    /// <summary>
    /// Play the current track. If there is no track, this will do nothing.
    /// </summary>
    public void Play();

    /// <summary>
    /// Pause the audio player.
    /// </summary>
    public void Pause();

    /// <summary>
    /// Stop the audio player, and clear the queue.
    /// </summary>
    public void Stop();

    /// <summary>
    /// Skip to the next track in the queue, if there is one. If there isn't one, then the player will stop and the
    /// queue will clear.
    /// </summary>
    public void Next();

    /// <summary>
    /// Skip back to the previous track in the queue. If at the beginning of the queue, the player will play the first
    /// track in the queue.
    /// </summary>
    public void Previous();

    /// <summary>
    /// Seek to a specific second in the current track. If there is no track, this will do nothing.
    /// </summary>
    /// <param name="second">The second to skip to.</param>
    public void Seek(int second);

    /// <summary>
    /// Queue a track at the given slot.
    /// </summary>
    /// <param name="path">The path to the track file.</param>
    /// <param name="slot">The <see cref="QueueSlot"/> to insert the track at.</param>
    public void QueueTrack(string path, QueueSlot slot);

    /// <summary>
    /// Queue multiple tracks at once.
    /// </summary>
    /// <param name="paths">The paths of all the tracks to queue.</param>
    /// <param name="slot">The <see cref="QueueSlot"/> to insert the tracks at.</param>
    /// <remarks>If <see cref="QueueSlot.Clear"/> is provided, the queue will be cleared, and <i>then</i> the tracks
    /// will be queued.</remarks>
    public void QueueTracks(IEnumerable<string> paths, QueueSlot slot);

    /// <summary>
    /// Change to a new track in the current queue.
    /// </summary>
    /// <param name="trackIndex">The index of the track to switch to.</param>
    public void ChangeTrack(int trackIndex);
    
    /// <summary>
    /// A track changed notification.
    /// </summary>
    /// <param name="info">The <see cref="TrackInfo"/> of the new track.</param>
    /// <param name="path">The path to the new track.</param>
    public delegate void OnTrackChanged(TrackInfo info, string path);

    /// <summary>
    /// A player state changed notification.
    /// </summary>
    /// <param name="state">The new player state.</param>
    public delegate void OnStateChanged(TrackState state);
}