using Glimpse.Api;

namespace Glimpse.Player;

public class GlimpseBase : IGlimpse
{
    public Logger Log;

    public ConfigManager Config;

    public AudioPlayer Player;
    
    public string DataDirectory { get; }

    public ILogger Logger => Log;

    public IConfigManager ConfigManager => Config;

    public IAudioPlayer AudioPlayer => Player;

    public GlimpseBase()
    {
#if DEBUG
        DataDirectory = "Data";
#else
        DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Glimpse");
#endif

        Log = new Logger(DataDirectory);
        
        Config = new ConfigManager(this);
        Player = new AudioPlayer(this);
    }
}