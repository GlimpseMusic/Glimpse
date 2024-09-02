using System;
using MixrSharp;
using MixrSharp.Devices;
using MixrSharp.Stream;

namespace Glimpse.Player;

public class AudioPlayer : IDisposable
{
    public PlayerSettings Settings;
    
    private Device _device;

    private readonly TrackInfo _defaultTrackInfo;
    
    private Track _activeTrack;

    public int ElapsedSeconds => _activeTrack?.ElapsedSeconds ?? 0;

    public int TrackLength => _activeTrack?.LengthInSeconds ?? 0;

    public TrackInfo TrackInfo => _activeTrack?.Info ?? _defaultTrackInfo;

    public TrackState TrackState => _activeTrack?.State ?? TrackState.Stopped;

    public AudioPlayer(PlayerSettings settings)
    {
        Settings = settings;
        
        _device = new SdlDevice(settings.SampleRate);

        _device.Context.MasterVolume = settings.Volume;
        
        _defaultTrackInfo = new TrackInfo("Unknown Title", "Unknown Artist", "Unknown Album");
        
        DiscordPresence.Initialize();
    }

    public void ChangeTrack(string path)
    {
        _activeTrack?.Dispose();

        TrackInfo info = TrackInfo.FromFile(path);
        
        AudioStream stream = new Flac(path);

        _activeTrack = new Track(_device.Context, stream, info, Settings);
    }

    public void Play()
    {
        DiscordPresence.SetPresence(_activeTrack.Info, _activeTrack.LengthInSeconds);
        _activeTrack.Play();
    }
    
    public void Pause()
    {
        _activeTrack.Pause();
    }

    public void Stop()
    {
        _activeTrack.Dispose();
        _activeTrack = null;
    }

    public void Dispose()
    {
        DiscordPresence.Deinitialize();
        
        _activeTrack?.Dispose();
        _device.Dispose();
    }
}