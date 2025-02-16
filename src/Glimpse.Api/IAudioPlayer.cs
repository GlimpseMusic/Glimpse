namespace Glimpse.Api;

public interface IAudioPlayer
{
    public event OnPlayerStateChanged StateChanged;
    
    public float Volume { get; set; }
    
    public double Speed { get; set; }
    
    public PlayerState State { get; }
    
    public TimeSpan ElapsedTime { get; }
    
    public TimeSpan CurrentTrackLength { get; }
    
    public TrackInfo CurrentTrack { get; }
    
    public IReadOnlyList<string> Queue { get; }

    public void Play();

    public void Pause();

    public void Stop();

    public delegate void OnPlayerStateChanged();
}