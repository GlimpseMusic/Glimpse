using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Glimpse.Player.Codecs;
using Glimpse.Player.Codecs.Flac;
using Glimpse.Player.Codecs.Mp3;
using Glimpse.Player.Codecs.Vorbis;
using Glimpse.Player.Codecs.Wav;
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

    public readonly List<Codec> Codecs;

    public readonly List<Plugin> Plugins;

    private AssemblyLoadContext _pluginsContext;
    
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

        Codecs = [new Mp3Codec(), new FlacCodec(), new VorbisCodec(), new WavCodec()];

        if (Directory.Exists("Plugins"))
        {
            _pluginsContext = new AssemblyLoadContext("Plugins");
            
            Plugins = new List<Plugin>();

            string pluginsLocation = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Plugins");
            
            foreach (string file in Directory.GetFiles(pluginsLocation, "*.dll", SearchOption.AllDirectories))
            {
                _pluginsContext.LoadFromAssemblyPath(file);
            }
        
            foreach (Assembly assembly in _pluginsContext.Assemblies)
            {
                Console.WriteLine(assembly);
                
                foreach (Type type in assembly.GetTypes()
                             .Where(type => type.IsAssignableTo(typeof(Plugin)) && type != typeof(Plugin)))
                {
                    Plugin plugin = (Plugin) Activator.CreateInstance(type);
                    if (plugin == null)
                        continue;

                    plugin.Initialize(this);

                    Plugins.Add(plugin);
                }
            }
        }
    }

    public void ChangeTrack(string path)
    {
        _activeTrack?.Dispose();

        TrackInfo info = TrackInfo.FromFile(path);

        CodecStream stream = CreateStreamFromFile(path);

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

    private CodecStream CreateStreamFromFile(string path)
    {
        string extension = Path.GetExtension(path);
        
        foreach (Codec codec in Codecs)
        {
            if (codec.FileIsSupported(path, extension))
                return codec.CreateStream(path);
        }

        throw new NotSupportedException($"File type '{extension}' not supported.");
    }

    public void Dispose()
    {
        if (Plugins != null)
        {
            foreach (Plugin plugin in Plugins)
                plugin.Dispose();
        }

        _activeTrack?.Dispose();
        _device.Dispose();
    }

    public delegate void OnTrackChanged(TrackInfo info);

    public delegate void OnStateChanged(TrackState state);
}