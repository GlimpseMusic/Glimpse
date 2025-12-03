using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;
using Glimpse.API;
using Glimpse.API.Database;
using Glimpse.Audio;
using Glimpse.Configs;
using Glimpse.Database;
using Glimpse.Platforms;
using Hexa.NET.ImGui;
using Silk.NET.SDL;
using Locale = Glimpse.Locales.Locale;
using Version = System.Version;

namespace Glimpse;

public class Glimpse : IGlimpse, IDisposable
{
    private Sdl _sdl;
    private List<Window> _windows;
    private Dictionary<uint, Window> _windowIds;
    private AssemblyLoadContext _pluginsContext;

    public Logger Logger;
    
    public Version Version;

    public ConfigManager ConfigManager;

    public GlimpseConfig Config;

    public Platform Platform;

    public AudioPlayer Player;

    public Locale Locale;

    public MusicDatabase Database;
    
    public Dictionary<string, IPlugin>? Plugins;

    public Window MainWindow => _windows[0];

    public void AddWindow(Window window)
    {
        window.Glimpse = this;
        uint id = window.Create(_sdl, Platform);
        _windows.Add(window);
        _windowIds.Add(id, window);
    }

    public unsafe void Run(Window window, string[] args)
    {
        Logger = new Logger();
        
        Version = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);
        Logger.Log($"Glimpse {Version}");
        
        Logger.Log("Creating config manager.");
        ConfigManager = new ConfigManager(Logger);
        
        Platform = Platform.AutoDetect();
        Logger.Log($"Detected platform {Platform.GetType()}");
        Platform.EnableDPIAwareness();
        
        Logger.Log("Loading player configuration.");
        if (!ConfigManager.TryGetConfig(GlimpseConfig.ConfigName, out Config))
        {
            Logger.Log("   ... Failed: Creating new config.");
            Config = new GlimpseConfig();
            ConfigManager.WriteConfig(GlimpseConfig.ConfigName, Config);
        }
        
        _sdl = Sdl.GetApi();
        _sdl.SetHint(Sdl.HintMouseFocusClickthrough, "1");
        _sdl.SetHint(Sdl.HintVideoAllowScreensaver, "1");
        
        if (_sdl.Init(Sdl.InitVideo | Sdl.InitEvents) < 0)
            throw new Exception("Failed to initialize SDL.");

        _windows = new List<Window>();
        _windowIds = new Dictionary<uint, Window>();

        Player = new AudioPlayer(Logger,
            new PlayerSettings(Config.SampleRate, Config.Volume, Config.SpeedAdjust, Config.AutoPlay));

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

        Database = new MusicDatabase(Logger, ConfigManager, Player);
        
        Logger.Log("Searching for 'Plugins' directory.");
        if (Directory.Exists("Plugins"))
        {
            _pluginsContext = new AssemblyLoadContext("Plugins");

            Plugins = [];

            string pluginsLocation = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Plugins");
            
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
                            Console.WriteLine("WARNING: Plugin requires different version of Glimpse. It may cause errors.");
                        
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
        
        AddWindow(window);
        
        if (args.Length > 0)
        {
            Player.QueueTrack(args[0], QueueSlot.AtEnd);
            Player.Play();
        }

        while (_windows.Count > 0)
        {
            Event winEvent;

            WindowFlags flags = (WindowFlags) _sdl.GetWindowFlags(_sdl.GetWindowFromID(_windowIds.First().Key));
            if ((flags & WindowFlags.Minimized) != 0 && _sdl.WaitEvent(&winEvent) != 0 ||
                (flags & WindowFlags.InputFocus) == 0 && _sdl.WaitEventTimeout(&winEvent, 250) != 0)
            {
                ProcessEvent(winEvent);
            }

            while (_sdl.PollEvent(&winEvent) != 0)
                ProcessEvent(winEvent);

            foreach (Window wnd in _windows)
            {
                wnd.SetActive();
                wnd.UpdateWindow();
                wnd.Present();
            }
        }
    }

    private ImGuiMouseButton? SdlButtonToImGui(uint button)
    {
        return button switch
        {
            Sdl.ButtonLeft => ImGuiMouseButton.Left,
            Sdl.ButtonRight => ImGuiMouseButton.Right,
            Sdl.ButtonMiddle => ImGuiMouseButton.Middle,
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
        Database.Dispose();
        Platform.Dispose();
        
        _sdl.Quit();
        _sdl.Dispose();
    }

    private void ProcessEvent(Event winEvent)
    {
        switch ((EventType) winEvent.Type)
        {
            case EventType.Windowevent:
            {
                switch ((WindowEventID) winEvent.Window.Event)
                {
                    case WindowEventID.Close:
                    {
                        Window wnd = _windowIds[winEvent.Window.WindowID];
                        wnd.Dispose();
                        _windowIds.Remove(winEvent.Window.WindowID);
                        _windows.Remove(wnd);
                        break;
                    }

                    case WindowEventID.Resized:
                    {
                        Window wnd = _windowIds[winEvent.Window.WindowID];
                        Size newSize = new Size(winEvent.Window.Data1, winEvent.Window.Data2);
                        wnd.SetActive();
                        wnd.Renderer.Resize(newSize);
                        
                        break;
                    }
                }

                break;
            }

            case EventType.Mousemotion:
            {
                Window wnd = _windowIds[winEvent.Motion.WindowID];
                ImGui.SetCurrentContext(wnd.Renderer.ImGui.ImGuiContext);
                
                ImGui.GetIO().AddMousePosEvent(winEvent.Motion.X, winEvent.Motion.Y);
                break;
            }

            case EventType.Mousebuttondown:
            {
                Window wnd = _windowIds[winEvent.Button.WindowID];
                ImGui.SetCurrentContext(wnd.Renderer.ImGui.ImGuiContext);
                
                if (SdlButtonToImGui(winEvent.Button.Button) is { } button)
                    ImGui.GetIO().AddMouseButtonEvent((int) button, true);
                break;
            }
            
            case EventType.Mousebuttonup:
            {
                Window wnd = _windowIds[winEvent.Button.WindowID];
                ImGui.SetCurrentContext(wnd.Renderer.ImGui.ImGuiContext);
                
                if (SdlButtonToImGui(winEvent.Button.Button) is { } button)
                    ImGui.GetIO().AddMouseButtonEvent((int) button, false);
                break;
            }

            case EventType.Mousewheel:
            {
                Window wnd = _windowIds[winEvent.Button.WindowID];
                ImGui.SetCurrentContext(wnd.Renderer.ImGui.ImGuiContext);
                
                ImGui.GetIO().AddMouseWheelEvent(winEvent.Wheel.X, winEvent.Wheel.Y);
                break;
            }
        }
    }

    Version IGlimpse.Version => Version;
    ILogger IGlimpse.Logger => Logger;
    IConfigManager IGlimpse.ConfigManager => ConfigManager;
    IAudioPlayer IGlimpse.Player => Player;
    ILocale? IGlimpse.Locale => Locale;
    IMusicDatabase? IGlimpse.Database => Database;
}