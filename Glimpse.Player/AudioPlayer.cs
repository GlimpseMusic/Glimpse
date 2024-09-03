using System;
using System.IO;
using Glimpse.Player.Configs;
using MixrSharp;
using MixrSharp.Devices;
using MixrSharp.Stream;

namespace Glimpse.Player;

public class AudioPlayer : IDisposable
{
    public PlayerConfig Config;
    
    private Device _device;

    private readonly TrackInfo _defaultTrackInfo;
    
    private Track _activeTrack;

    public int ElapsedSeconds => _activeTrack?.ElapsedSeconds ?? 0;

    public int TrackLength => _activeTrack?.LengthInSeconds ?? 0;

    public TrackInfo TrackInfo => _activeTrack?.Info ?? _defaultTrackInfo;

    public TrackState TrackState => _activeTrack?.State ?? TrackState.Stopped;

    public AudioPlayer()
    {
        if (!IConfig.TryGetConfig("Player", out Config))
        {
            Config = new PlayerConfig();
            IConfig.WriteConfig("Player", Config);
        }

        _device = new SdlDevice(Config.SampleRate);

        _device.Context.MasterVolume = Config.Volume;
        
        _defaultTrackInfo = new TrackInfo("Unknown Title", "Unknown Artist", "Unknown Album");
        
        DiscordPresence.Initialize();
    }

    public void ChangeTrack(string path)
    {
        _activeTrack?.Dispose();

        TrackInfo info = TrackInfo.FromFile(path);

        AudioStream stream = CreateStreamFromFile(path);

        _activeTrack = new Track(_device.Context, stream, info, Config);
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

    private static AudioStream CreateStreamFromFile(string path)
    {
        string extension = Path.GetExtension(path);
        
        // TODO: This isn't very robust. But it works for now.
        switch (extension)
        {
            case ".mp3":
                return new Mp3(path);
            case ".ogg":
                return new Vorbis(path);
            case ".wav":
                return new Wav(path);
            case ".flac":
                return new Flac(path);
            
            default:
                throw new NotSupportedException($"Files with type '{extension}' are not supported.");
        }
    }

    public void Dispose()
    {
        DiscordPresence.Deinitialize();
        
        _activeTrack?.Dispose();
        _device.Dispose();
    }
}