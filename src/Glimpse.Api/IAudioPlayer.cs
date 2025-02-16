namespace Glimpse.Api;

public interface IAudioPlayer
{
    public PlayerState State { get; }
    
    public TimeSpan ElapsedTime { get; }
    
    public TimeSpan CurrentTrackLength { get; }
    
    public TrackInfo CurrentTrack { get; }

    public void Play();

    public void Pause();

    public void Stop();
}