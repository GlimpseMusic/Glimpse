using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Glimpse.Api;
using Glimpse.Player.Codecs;
using Glimpse.Player.Codecs.Flac;
using Glimpse.Player.Codecs.Mp3;
using Glimpse.Player.Codecs.Vorbis;
using Glimpse.Player.Codecs.Wav;
using Glimpse.Player.Configs;
using MixrSharp;
using MixrSharp.Devices;
using MixrSharp.Stream;

namespace Glimpse.Player;

public class AudioPlayer : IAudioPlayer, IDisposable
{
    public event IAudioPlayer.OnTrackChanged TrackChanged = delegate { };

    public event IAudioPlayer.OnStateChanged StateChanged = delegate { };

    private readonly Logger _logger;
    
    private AssemblyLoadContext _pluginsContext;
    
    private Device _device;

    private readonly TrackInfo _defaultTrackInfo;
    
    private Track _activeTrack;

    private int _currentTrackIndex;
    private int _currentQueueIndex;

    public readonly PlayerConfig Config;

    public readonly List<Codec> Codecs;

    public readonly List<Plugin> Plugins;

    public readonly List<string> QueuedTracksInternal;
    
    public int ElapsedSeconds => _activeTrack?.ElapsedSeconds ?? 0;

    public int TrackLength => _activeTrack?.LengthInSeconds ?? 0;

    public TrackInfo CurrentTrack => _activeTrack?.Info ?? _defaultTrackInfo;

    public TrackState State => _activeTrack?.State ?? TrackState.Stopped;

    public int CurrentTrackIndex => _currentTrackIndex;

    public IEnumerable<string> QueuedTracks
    {
        get
        {
            foreach (string path in QueuedTracksInternal)
                yield return path;
        }
    }

    public AudioPlayer(GlimpseBase glimpse)
    {
        _logger = glimpse.Log;
        
        _logger.Log("Loading player configuration.");
        if (!glimpse.ConfigManager.TryGetConfig("Player", out Config))
        {
            _logger.Log("   ... Failed: Creating new config.");
            Config = new PlayerConfig();
            glimpse.ConfigManager.WriteConfig("Player", Config);
        }

        _logger.Log("Creating SdlDevice.");
        _device = new SdlDevice(Config.SampleRate);
        _device.Context.MasterVolume = Config.Volume;
        
        _defaultTrackInfo = new TrackInfo(null, "Unknown Title", "Unknown Artist", "Unknown Album", null);

        _logger.Log("Initializing codecs.");
        Codecs = [new Mp3Codec(), new FlacCodec(), new VorbisCodec(), new WavCodec()];

        QueuedTracksInternal = new List<string>();

        _logger.Log("Searching for 'Plugins' directory.");
        if (Directory.Exists("Plugins"))
        {
            _pluginsContext = new AssemblyLoadContext("Plugins");
            
            Plugins = new List<Plugin>();

            string pluginsLocation = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Plugins");
            
            _logger.Log($"Searching for plugins in {pluginsLocation}");
            foreach (string file in Directory.GetFiles(pluginsLocation, "*.dll", SearchOption.AllDirectories))
            {
                try
                {
                    _logger.Log($"Loading assembly from {file}");
                    _pluginsContext.LoadFromAssemblyPath(file);
                }
                catch (BadImageFormatException e)
                {
                    _logger.Log($"Failed to load DLL: {e}");
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
                
                _logger.Log($"Plugin {assembly} loaded.");
                
                foreach (Type type in assembly.GetTypes().Where(type => type.IsAssignableTo(typeof(Plugin))))
                {
                    _logger.Log($"Initializing plugin {type}");
                    
                    Plugin plugin = (Plugin) Activator.CreateInstance(type);
                    if (plugin == null)
                        continue;
                    
                    _logger.Log("    ... Initialize()");
                    plugin.Initialize(glimpse);

                    Plugins.Add(plugin);
                }
            }
        }
    }
    
    public void QueueTrack(string path, QueueSlot slot)
    {
        _logger.Log($"Queueing track {path}");

        bool isFirstQueue = QueuedTracksInternal.Count == 0;

        switch (slot)
        {
            case QueueSlot.AtEnd:
                QueuedTracksInternal.Add(path);
                break;
            case QueueSlot.Queue:
                InsertTrackAtIndex(_currentTrackIndex + ++_currentQueueIndex, path);
                break;
            case QueueSlot.NextTrack:
                InsertTrackAtIndex(_currentTrackIndex + 1, path);
                _currentQueueIndex++;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(slot), slot, null);
        }
        
        if (isFirstQueue)
            ChangeTrack(0);
    }

    public void QueueTracks(IEnumerable<string> paths, QueueSlot slot)
    {
        if (slot == QueueSlot.Clear)
        {
            QueuedTracksInternal.Clear();
            slot = QueueSlot.AtEnd;
        }
        
        foreach (string path in paths)
            QueueTrack(path, slot);
    }

    public void ChangeTrack(int queueIndex)
    {
        if (queueIndex >= QueuedTracksInternal.Count || queueIndex < 0)
            throw new Exception("Cannot queue track that is not in the queue.");
        
        _activeTrack?.Dispose();
        _currentTrackIndex = queueIndex;

        string path = QueuedTracksInternal[queueIndex];
        
        _logger.Log($"Creating codec stream from file {path}");

        CodecStream stream = CreateStreamFromFile(path);
        TrackInfo info = stream.TrackInfo;

        _activeTrack = new Track(_logger, _device.Context, stream, info, Config, OnTrackFinish);

        TrackChanged(info, path);
    }

    public void Play()
    {
        _logger.Log("Start playback.");
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
        _activeTrack?.Dispose();
        _activeTrack = null;
        
        QueuedTracksInternal.Clear();
        _currentTrackIndex = 0;
        
        StateChanged(TrackState.Stopped);
    }

    public void Next()
    {
        _currentTrackIndex++;

        if (_currentTrackIndex >= QueuedTracksInternal.Count)
        {
            Stop();
            return;
        }

        _currentQueueIndex--;
        if (_currentQueueIndex < 0)
            _currentQueueIndex = 0;
        
        ChangeTrack(_currentTrackIndex);
        Play();
    }

    public void Previous()
    {
        _currentTrackIndex--;
        
        if (_currentTrackIndex < 0)
            _currentTrackIndex = 0;

        if (_currentQueueIndex != 0)
            _currentQueueIndex++;
        
        ChangeTrack(_currentTrackIndex);
        Play();
    }

    public void Seek(int second)
    {
        _activeTrack.Seek(second);
        StateChanged(State);
    }

    public bool FileIsSupported(string path, out Codec outCodec)
    {
        string extension = Path.GetExtension(path);
        foreach (Codec codec in Codecs)
        {
            if (codec.FileIsSupported(path, extension))
            {
                outCodec = codec;
                return true;
            }
        }

        outCodec = null;
        return false;
    }

    public CodecStream CreateStreamFromFile(string path)
    {
        _logger.Log("Checking for codec support.");
        if (FileIsSupported(path, out Codec codec))
            return codec.CreateStream(path);

        throw new NotSupportedException($"File type '{Path.GetExtension(path)}' not supported.");
    }

    private void InsertTrackAtIndex(int index, string path)
    {
        if (index >= QueuedTracksInternal.Count)
            QueuedTracksInternal.Add(path);
        else
            QueuedTracksInternal.Insert(index, path);
    }

    private void OnTrackFinish()
    {
        Next();
    }

    public void Dispose()
    {
        if (Plugins != null)
        {
            _logger.Log("Disposing all plugins.");
            foreach (Plugin plugin in Plugins)
            {
                _logger.Log($"Disposing plugin {plugin.GetType()}");
                plugin.Dispose();
            }
        }

        _logger.Log("Disposing track.");
        _activeTrack?.Dispose();
        _logger.Log("Disposing device.");
        _device.Dispose();
    }
}