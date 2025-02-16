using Glimpse.Api;

namespace Glimpse.Player;

public class AudioPlayer : IAudioPlayer
{
    public float Volume { get; set; }
    
    public double Speed { get; set; }
    
    public PlayerState State { get; }
    
    public TimeSpan ElapsedTime { get; }
    
    public TimeSpan CurrentTrackLength { get; }
    
    public TrackInfo CurrentTrack { get; }
    
    public void Play()
    {
        throw new NotImplementedException();
    }
    
    public void Pause()
    {
        throw new NotImplementedException();
    }
    
    public void Stop()
    {
        throw new NotImplementedException();
    }
}