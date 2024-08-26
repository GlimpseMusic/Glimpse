using System;
using MixrSharp;
using MixrSharp.Devices;
using MixrSharp.Stream;

namespace Glimpse.Player;

public class AudioPlayer : IDisposable
{
    private Device _device;
    
    private Track _activeTrack;

    public int ElapsedSeconds => _activeTrack?.ElapsedSeconds ?? 0;

    public int TrackLength => _activeTrack?.LengthInSeconds ?? 0;

    public TrackState TrackState => _activeTrack?.State ?? TrackState.Stopped;

    public AudioPlayer(PlayerSettings settings)
    {
        _device = new SdlDevice(settings.SampleRate);
    }

    public void ChangeTrack(string path)
    {
        _activeTrack?.Dispose();

        AudioStream stream = new Flac(path);

        _activeTrack = new Track(_device.Context, stream);
    }

    public void Play()
    {
        _activeTrack.Play();
    }
    
    public void Pause()
    {
        _activeTrack.Pause();
    }

    public void Stop()
    {
        _activeTrack.Dispose();
    }

    public void Dispose()
    {
        _activeTrack?.Dispose();
        _device.Dispose();
    }
}