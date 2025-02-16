namespace Glimpse.Api;

public interface IAudioPlayer
{
    public float Volume { get; set; }
    
    public double Speed { get; set; }
    
    public PlayerState State { get; }
    
    public TimeSpan ElapsedTime { get; }
    
    public TimeSpan CurrentTrackLength { get; }
    
    public TrackInfo CurrentTrack { get; }

    public void Play();

    public void Pause();

    public void Stop();
}