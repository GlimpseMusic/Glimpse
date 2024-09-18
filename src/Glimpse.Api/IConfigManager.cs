namespace Glimpse.Api;

public interface IConfigManager
{
    public bool TryGetConfig<T>(string name, out T config) where T : IConfig;

    public void WriteConfig(string name, IConfig config);
}