using System;
using System.IO;
using Glimpse.Player.Configs;
using MixrSharp;
using MixrSharp.Devices;
using MixrSharp.Stream;

namespace Glimpse.Player;

public static class AudioPlayer
{
    public static PlayerConfig Config;
    
    private static Device _device;

    private static TrackInfo _defaultTrackInfo;
    
    private static Track _activeTrack;

    public static int ElapsedSeconds => _activeTrack?.ElapsedSeconds ?? 0;

    public static int TrackLength => _activeTrack?.LengthInSeconds ?? 0;

    public static TrackInfo TrackInfo => _activeTrack?.Info ?? _defaultTrackInfo;

    public static TrackState TrackState => _activeTrack?.State ?? TrackState.Stopped;

    public static void Initialize()
    {
        if (!IConfig.TryGetConfig("Player", out Config))
        {
            Config = new PlayerConfig();
            IConfig.WriteConfig("Player", Config);
        }

        _device = new SdlDevice(Config.SampleRate);
        _device.Context.MasterVolume = Config.Volume;
        
        _defaultTrackInfo = new TrackInfo("Unknown Title", "Unknown Artist", "Unknown Album", null);
        
        DiscordPresence.Initialize();
    }

    public static void ChangeTrack(string path)
    {
        _activeTrack?.Dispose();

        TrackInfo info = TrackInfo.FromFile(path);

        AudioStream stream = CreateStreamFromFile(path);

        _activeTrack = new Track(_device.Context, stream, info, Config);
    }

    public static void Play()
    {
        DiscordPresence.SetPresence(_activeTrack.Info, _activeTrack.LengthInSeconds);
        _activeTrack.Play();
    }
    
    public static void Pause()
    {
        _activeTrack.Pause();
    }

    public static void Stop()
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

    public static void Dispose()
    {
        DiscordPresence.Deinitialize();
        
        _activeTrack?.Dispose();
        _device.Dispose();
    }
}