using System;
using MixrSharp;
using MixrSharp.Devices;

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

    public void ChangeTrack(string path)
    {
        _activeTrack?.Dispose();
    }

    public void Dispose()
    {
        _device.Dispose();
    }
}