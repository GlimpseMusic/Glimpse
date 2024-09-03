using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Glimpse.Player.Configs;
using Glimpse.Player.Plugins;
using MixrSharp;
using MixrSharp.Devices;
using MixrSharp.Stream;

namespace Glimpse.Player;

public class AudioPlayer : IDisposable
{
    public event OnTrackChanged TrackChanged = delegate { };

    public event OnStateChanged StateChanged = delegate { };

    public readonly PlayerConfig Config;

    public readonly List<Plugin> Plugins;
    
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
        
        _defaultTrackInfo = new TrackInfo("Unknown Title", "Unknown Artist", "Unknown Album", null);

        Plugins = new List<Plugin>();
        
        foreach (Type type in Assembly.GetExecutingAssembly().GetTypes()
                     .Where(type => type.IsAssignableTo(typeof(Plugin)) && type != typeof(Plugin)))
        {
            Plugin plugin = (Plugin) Activator.CreateInstance(type);
            if (plugin == null)
                continue;
            
            plugin.Initialize(this);
            
            Plugins.Add(plugin);
        }
    }

    public void ChangeTrack(string path)
    {
        _activeTrack?.Dispose();

        TrackInfo info = TrackInfo.FromFile(path);

        AudioStream stream = CreateStreamFromFile(path);

        _activeTrack = new Track(_device.Context, stream, info, Config);

        TrackChanged(info);
    }

    public void Play()
    {
        _activeTrack.Play();
        StateChanged(TrackState.Playing);
    }
    
    public void Pause()
    {
        _activeTrack.Pause();
        StateChanged(TrackState.Paused);
    }

    public void Stop()
    {
        _activeTrack.Dispose();
        _activeTrack = null;
        StateChanged(TrackState.Stopped);
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
        foreach (Plugin plugin in Plugins)
            plugin.Dispose();
        
        _activeTrack?.Dispose();
        _device.Dispose();
    }

    public delegate void OnTrackChanged(TrackInfo info);

    public delegate void OnStateChanged(TrackState state);
}