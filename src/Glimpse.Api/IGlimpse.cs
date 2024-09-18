namespace Glimpse.Api;

public interface IGlimpse
{
    public string DataDirectory { get; }
    
    public IConfigManager ConfigManager { get; }
    
    public IAudioPlayer AudioPlayer { get; }
}