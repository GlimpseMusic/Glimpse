using System;
using MixrSharp;
using MixrSharp.Devices;
using MixrSharp.Stream;

namespace Glimpse.Player;

public class AudioPlayer : IDisposable
{
    private Device _device;
    
    private Track _activeTrack;

    public Track Track => _activeTrack;

    public AudioPlayer(PlayerSettings settings)
    {
        _device = new SdlDevice(settings.SampleRate);
    }

    public void PlayTrack(string path)
    {
        _activeTrack?.Dispose();

        AudioStream stream = new Flac(path);

        _activeTrack = new Track(_device.Context, stream);
        _activeTrack.Play();
    }

    public void Stop()
    {
        _activeTrack.Dispose();
    }

    public void Dispose()
    {
        _device.Dispose();
    }
}