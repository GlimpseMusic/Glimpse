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

    public readonly List<string> QueuedTracks;

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
        Logger.Log("Loading player configuration.");
        if (!IConfig.TryGetConfig("Player", out Config))
        {
            Logger.Log("   ... Failed: Creating new config.");
            Config = new PlayerConfig();
            IConfig.WriteConfig("Player", Config);
        }

        Logger.Log("Creating SdlDevice.");
        _device = new SdlDevice(Config.SampleRate);
        _device.Context.MasterVolume = Config.Volume;
        
        _defaultTrackInfo = new TrackInfo("Unknown Title", "Unknown Artist", "Unknown Album", null);

        Logger.Log("Initializing codecs.");
        Codecs = [new Mp3Codec(), new FlacCodec(), new VorbisCodec(), new WavCodec()];

        Logger.Log("Searching for 'Plugins' directory.");
        if (Directory.Exists("Plugins"))
        {
            _pluginsContext = new AssemblyLoadContext("Plugins");
            
            Plugins = new List<Plugin>();

            string pluginsLocation = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Plugins");
            
            Logger.Log($"Searching for plugins in {pluginsLocation}");
            foreach (string file in Directory.GetFiles(pluginsLocation, "*.dll", SearchOption.AllDirectories))
            {
                try
                {
                    Logger.Log($"Loading assembly from {file}");
                    _pluginsContext.LoadFromAssemblyPath(file);
                }
                catch (BadImageFormatException)
                {
                    // If this is thrown then it's likely a native DLL.
                }
            }

            AssemblyName currentName = Assembly.GetAssembly(typeof(AudioPlayer))?.GetName();
            
            foreach (Assembly assembly in _pluginsContext.Assemblies)
            {
                foreach (AssemblyName name in assembly.GetReferencedAssemblies())
                {
                    if (name.Name == currentName.Name)
                    {
                        if (name.Version != currentName.Version)
                            Console.WriteLine("WARNING: Plugin requires different version of Glimpse. It may cause errors.");
                        
                        goto ASSEMBLY_GOOD;
                    }
                }
                
                continue;
                
                ASSEMBLY_GOOD: ;
                
                Logger.Log($"Plugin {assembly} loaded.");
                
                foreach (Type type in assembly.GetTypes().Where(type => type.IsAssignableTo(typeof(Plugin))))
                {
                    Logger.Log($"Initializing plugin {type}");
                    
                    Plugin plugin = (Plugin) Activator.CreateInstance(type);
                    if (plugin == null)
                        continue;
                    
                    Logger.Log("    ... Initialize()");
                    plugin.Initialize(this);

                    Plugins.Add(plugin);
                }
            }
        }
    }

    public void ChangeTrack(string path)
    {
        _activeTrack?.Dispose();
        
        Logger.Log($"Creating codec stream from file {path}");
        CodecStream stream = CreateStreamFromFile(path);
        Logger.Log($"Created {stream.GetType()}.");
        TrackInfo info = stream.TrackInfo;

        _activeTrack = new Track(_device.Context, stream, info, Config);

        TrackChanged(info);
    }

    public void Play()
    {
        Logger.Log("Start playback.");
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
        Logger.Log($"File extension: {extension}");
        
        Logger.Log("Checking for codec support.");
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
            Logger.Log("Disposing all plugins.");
            foreach (Plugin plugin in Plugins)
            {
                Logger.Log($"Disposing plugin {plugin.GetType()}");
                plugin.Dispose();
            }
        }

        Logger.Log("Disposing track.");
        _activeTrack?.Dispose();
        Logger.Log("Disposing device.");
        _device.Dispose();
    }

    public delegate void OnTrackChanged(TrackInfo info);

    public delegate void OnStateChanged(TrackState state);
}