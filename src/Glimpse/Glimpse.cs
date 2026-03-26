using System.Drawing;
using System.Globalization;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Loader;
using Glimpse.API;
using Glimpse.Audio;
using Glimpse.Configs;
using Glimpse.Database;
using Glimpse.Platforms;
using Hexa.NET.ImGui;
using SDL3;
using Locale = Glimpse.Locales.Locale;
using Version = System.Version;

namespace Glimpse;

public class Glimpse : IGlimpse, IDisposable
{
#if DEBUG
    public const string PipeServerName = "GlimpsePlayerDEBUG";
#else
    public const string PipeServerName = "GlimpsePlayer";
#endif
    
    private List<Window> _windows;
    private Dictionary<uint, Window> _windowIds;
    private AssemblyLoadContext _pluginsContext;
    private int _mainThreadID;
    private SDL.EventFilter _eventFilter;
    
    private NamedPipeServerStream _pipeServer;
    
    private bool _shouldFocusWindow;
    private string? _playFile;

    public Logger Logger;

    public SemVer Version;
    
    public ConfigManager ConfigManager;

    public GlimpseConfig Config;

    public Platform Platform;

    public AudioPlayer Player;

    public Locale Locale;

    public DatabaseManager? Database;
    
    public Dictionary<string, IPlugin>? Plugins;

    public Window MainWindow => _windows[0];

    public void AddWindow(Window window)
    {
        window.Glimpse = this;
        uint id = window.Create(Platform);
        _windows.Add(window);
        _windowIds.Add(id, window);
    }

    public void Run(Window window, string[] args)
    {
        Logger = new Logger();
        
        _pipeServer = new NamedPipeServerStream(PipeServerName, PipeDirection.In);
        _pipeServer.BeginWaitForConnection(OnPipeServerConnection, this);

        // get the ass
        Assembly? ass = Assembly.GetEntryAssembly();
        // no ass
        if (ass == null)
            Version = new SemVer();
        else
        {
            // i love as- no nevermind
            AssemblyInformationalVersionAttribute? infoVersion =
                ass.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

#if !DEBUG
            try
            {
#endif
                Version = new SemVer(infoVersion!.InformationalVersion);
#if !DEBUG
            }
            catch (Exception e)
            {
                Logger.Log($"Could not parse version number! Using fallback. {e}.");

                Version? assVersion = ass.GetName().Version;
                // ass does not have a version
                if (assVersion == null)
                    Version = new SemVer();
                else
                    Version = new SemVer(assVersion.Major, assVersion.Minor, assVersion.Build, assVersion.Revision);
            }
#endif
        }
            
        Logger.Log($"Glimpse {Version}");
        
        Logger.Log("Creating config manager.");
        ConfigManager = new ConfigManager(Logger);
        
        Platform = Platform.AutoDetect();
        Logger.Log($"Detected platform {Platform.GetType()}");
        
        Logger.Log("Loading player configuration.");
        if (!ConfigManager.TryGetConfig(GlimpseConfig.ConfigName, out Config))
        {
            Logger.Log("   ... Failed: Creating new config.");
            Config = new GlimpseConfig();
            ConfigManager.WriteConfig(GlimpseConfig.ConfigName, Config);
        }
        
        SDL.SetHint(SDL.Hints.MouseFocusClickthrough, "1");
        SDL.SetHint(SDL.Hints.VideoAllowScreensaver, "1");
        
        if (!SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Events))
            throw new Exception("Failed to initialize SDL.");

        _windows = new List<Window>();
        _windowIds = new Dictionary<uint, Window>();

        Player = new AudioPlayer(Logger, new PlayerSettings(Config.SampleRate, Config.Volume, Config.SpeedAdjust));

        Logger.Log("Loading locales.");
        Locale.LoadAvailableLocales();
        
        const string defaultLocale = "en-gb";
        string requestedLocale = Config.Language ?? CultureInfo.CurrentUICulture.Name.ToLower();
        Logger.Log($"Requesting locale '{requestedLocale}'.");
        if (Locale.AvailableLocales.ContainsKey(requestedLocale))
        {
            Locale = Locale.LoadLocale(requestedLocale);
            Config.Language = requestedLocale;
        }
        else
        {
            Logger.Log($"Requested locale '{requestedLocale}' is not available. Loading default locale '{defaultLocale}'.");
            Locale = Locale.LoadLocale(defaultLocale);
            Config.Language = defaultLocale;
        }

        if (!ConfigManager.TryGetConfig(DatabaseManager.DatabaseName, out Database))
        {
            Database = new DatabaseManager();
            ConfigManager.WriteConfig(DatabaseManager.DatabaseName, Database);
        }

        Database!.Logger = Logger;
        Database!.Refresh();
        
        Logger.Log("Searching for 'Plugins' directory.");
        string pluginsLocation = GetPath("Plugins");
        if (Directory.Exists(pluginsLocation))
        {
            _pluginsContext = new AssemblyLoadContext("Plugins");

            Plugins = [];
            
            Logger.Log($"Searching for plugins in {pluginsLocation}");
            foreach (string file in Directory.GetFiles(pluginsLocation, "*.dll", SearchOption.AllDirectories))
            {
                try
                {
                    Logger.Log($"Loading assembly from {file}");
                    _pluginsContext.LoadFromAssemblyPath(file);
                }
                catch (BadImageFormatException e)
                {
                    Logger.Log($"Failed to load DLL: {e}");
                    // If this is thrown then it's likely a native DLL.
                }
            }

            AssemblyName currentName = Assembly.GetAssembly(typeof(IPlugin))?.GetName();
            
            foreach (Assembly assembly in _pluginsContext.Assemblies)
            {
                foreach (AssemblyName name in assembly.GetReferencedAssemblies())
                {
                    if (name.Name == currentName.Name)
                    {
                        if (name.Version != currentName.Version)
                            Logger.Log($"WARNING: Plugin {name.Name} requires different version of Glimpse (current version: {currentName.Version}, requires: {name.Version}). It may cause errors.");
                        
                        goto ASSEMBLY_GOOD;
                    }
                }
                
                continue;
                
                ASSEMBLY_GOOD: ;
                
                Logger.Log($"Plugin {assembly} loaded.");
                
                foreach (Type type in assembly.GetTypes().Where(type => type.IsAssignableTo(typeof(IPlugin))))
                {
                    Logger.Log($"Initializing plugin {type}");
                    
                    IPlugin? plugin = (IPlugin?) Activator.CreateInstance(type);
                    if (plugin == null)
                        continue;

                    string assemblyName = assembly.GetName().Name;

                    if (Config.EnabledPlugins.Contains(assemblyName))
                    {
                        Logger.Log("    ... Initialize()");
                        plugin.Initialize(this);
                    }

                    Plugins.Add(assemblyName, plugin);
                }
            }
        }

        _mainThreadID = Environment.CurrentManagedThreadId;
        _eventFilter = WindowExposedEventWatch;
        
        AddWindow(window);
        SDL.AddEventWatch(_eventFilter, 0);
        
        if (args.Length > 0)
        {
            Player.QueueTrack(args[0], QueueSlot.Clear);
            Player.Play();
        }

        while (_windows.Count > 0)
        {
            SDL.Event winEvent;

            SDL.WindowFlags flags = SDL.GetWindowFlags(SDL.GetWindowFromID(_windowIds.First().Key));
            if ((flags & SDL.WindowFlags.Minimized) != 0 && SDL.WaitEvent(out winEvent) ||
                (flags & SDL.WindowFlags.InputFocus) == 0 && SDL.WaitEventTimeout(out winEvent, 250))
            {
                ProcessEvent(winEvent);
            }

            while (SDL.PollEvent(out winEvent))
                ProcessEvent(winEvent);

            if (_shouldFocusWindow)
            {
                _shouldFocusWindow = false;
                IntPtr handle = MainWindow.Handle;
                SDL.RaiseWindow(handle);
            }

            if (_playFile != null)
            {
                Player.QueueTrack(_playFile, QueueSlot.Clear);
                _playFile = null;
            }

            foreach (Window wnd in _windows)
            {
                wnd.SetActive();
                wnd.UpdateWindow();
                wnd.Present();
            }
        }
        
        SDL.RemoveEventWatch(_eventFilter, 0);
    }

    private bool WindowExposedEventWatch(IntPtr userdata, ref SDL.Event @event)
    {
        int threadId = Environment.CurrentManagedThreadId;
        if (threadId != _mainThreadID)
            //throw new Exception($"Event {(SDL.EventType) @event.Type} was sent on a separate thread.");
            Logger.Log($"Event {(SDL.EventType) @event.Type} was sent on a separate thread!!! Thread ID: {threadId}, Main Thread ID: {_mainThreadID}");
        
        switch ((SDL.EventType) @event.Type)
        {
            case SDL.EventType.WindowResized:
            {
                Logger.Log("Window Resized");
                Window wnd = _windowIds[@event.Window.WindowID];
                wnd.SetActive();
                wnd.Renderer.Resize(wnd.Size);
                Logger.Log("done.");
                break;
            }
            
            case SDL.EventType.WindowDisplayScaleChanged:
            {
                Logger.Log("Window Display Scale Changed");
                Window wnd = _windowIds[@event.Window.WindowID];
                wnd.SetActive();
                Size winSize = wnd.Size;
                float scaleDiff = SDL.GetWindowDisplayScale(wnd.Handle) / wnd.Scale;
                wnd.Size = new Size((int) (winSize.Width * scaleDiff), (int) (winSize.Height * scaleDiff));
                wnd.Renderer.Resize(wnd.Size);
                wnd.NotifyScaleChanged();
                Logger.Log("done.");
                break;
            }
            
            case SDL.EventType.WindowExposed:
            {
                bool isResized = @event.Window.Data1 == 1;
                if (isResized)
                {
                    Logger.Log("Window Exposed");
                    foreach (Window wnd in _windows)
                    {
                        wnd.SetActive();
                        wnd.UpdateWindow();
                        wnd.Present();
                    }

                    Logger.Log("done.");
                }

                break;
            }
            
            default:
                return true;
        }

        return false;
    }

    private ImGuiMouseButton? SdlButtonToImGui(uint button)
    {
        return button switch
        {
            SDL.ButtonLeft => ImGuiMouseButton.Left,
            SDL.ButtonRight => ImGuiMouseButton.Right,
            SDL.ButtonMiddle => ImGuiMouseButton.Middle,
            _ => null
        };
    }
    
    public void Dispose()
    {
        ConfigManager.WriteConfig(GlimpseConfig.ConfigName, Config);
        
        if (Plugins != null)
        {
            Logger.Log("Disposing all plugins.");
            foreach ((string name, IPlugin plugin) in Plugins)
            {
                Logger.Log($"Disposing plugin {name}");
                plugin.Dispose();
            }
        }
        
        Player.Dispose();
        ConfigManager.WriteConfig(DatabaseManager.DatabaseName, Database);
        Platform.Dispose();
        _pipeServer.Close();
        
        SDL.Quit();
    }

    private void ProcessEvent(SDL.Event winEvent)
    {
        switch ((SDL.EventType) winEvent.Type)
        {
            case SDL.EventType.WindowCloseRequested:
            {
                if (!_windowIds.TryGetValue(winEvent.Window.WindowID, out Window wnd))
                    break;
                
                wnd.Dispose();
                _windowIds.Remove(winEvent.Window.WindowID);
                _windows.Remove(wnd);
                break;
            }

            case SDL.EventType.Quit:
            {
                foreach (Window window in _windows)
                    window.Dispose();

                _windows.Clear();
                _windowIds.Clear();

                break;
            }

            case SDL.EventType.MouseMotion:
            {
                Window wnd = _windowIds[winEvent.Motion.WindowID];
                ImGui.SetCurrentContext(wnd.Renderer.ImGui.ImGuiContext);

                ImGui.GetIO().AddMousePosEvent(winEvent.Motion.X * MainWindow.PixelDensity,
                    winEvent.Motion.Y * MainWindow.PixelDensity);
                break;
            }

            case SDL.EventType.MouseButtonDown:
            {
                Window wnd = _windowIds[winEvent.Button.WindowID];
                ImGui.SetCurrentContext(wnd.Renderer.ImGui.ImGuiContext);
                
                if (SdlButtonToImGui(winEvent.Button.Button) is { } button)
                    ImGui.GetIO().AddMouseButtonEvent((int) button, true);
                break;
            }
            
            case SDL.EventType.MouseButtonUp:
            {
                Window wnd = _windowIds[winEvent.Button.WindowID];
                ImGui.SetCurrentContext(wnd.Renderer.ImGui.ImGuiContext);
                
                if (SdlButtonToImGui(winEvent.Button.Button) is { } button)
                    ImGui.GetIO().AddMouseButtonEvent((int) button, false);
                break;
            }

            case SDL.EventType.MouseWheel:
            {
                Window wnd = _windowIds[winEvent.Button.WindowID];
                ImGui.SetCurrentContext(wnd.Renderer.ImGui.ImGuiContext);
                
                ImGui.GetIO().AddMouseWheelEvent(-winEvent.Wheel.X, winEvent.Wheel.Y);
                break;
            }
        }
    }

    public static string GetPath(string path)
    {
        return Path.Combine(AppContext.BaseDirectory, path);
    }

    private void OnPipeServerConnection(IAsyncResult asyncResult)
    {
        try
        {
            _pipeServer.EndWaitForConnection(asyncResult);
        }
        catch (SocketException)
        {
            return;
        }
        catch (IOException)
        {
            return;
        }

        _shouldFocusWindow = true;

        int b = _pipeServer.ReadByte();
        if (b != -1)
        {
            Program.CommunicationFlags flags = (Program.CommunicationFlags) b;
            BinaryReader reader = new BinaryReader(_pipeServer);

            if ((flags & Program.CommunicationFlags.PlayFile) != 0)
                _playFile = reader.ReadString();
        }
            
        _pipeServer.Disconnect();

        _pipeServer.BeginWaitForConnection(OnPipeServerConnection, this);
    }

    SemVer IGlimpse.Version => Version;
    ILogger IGlimpse.Logger => Logger;
    IConfigManager IGlimpse.ConfigManager => ConfigManager;
    IAudioPlayer IGlimpse.Player => Player;
    ILocale? IGlimpse.Locale => Locale;
}