using Glimpse.API.Database;

namespace Glimpse.API;

public interface IGlimpse
{
    public Version Version { get; }
    
    public ILogger Logger { get; }
    
    public IConfigManager ConfigManager { get; }
    
    public IAudioPlayer Player { get; }
    
    public ILocale? Locale { get; }
    
    public IMusicDatabase? Database { get; }
}