using System.IO;
using Glimpse.Api;
using Newtonsoft.Json;

namespace Glimpse.Player;

public class ConfigManager : IConfigManager
{
    private readonly Logger _logger;
    private readonly string _configDirectory;

    public ConfigManager(GlimpseBase glimpseBase)
    {
        _logger = glimpseBase.Log;
        _configDirectory = glimpseBase.DataDirectory;
    }
    
    public bool TryGetConfig<T>(string name, out T config) where T : IConfig
    {
        string fullPath = Path.Combine(_configDirectory, $"{name}.json");
        _logger.Log($"Trying to load config {fullPath}.");

        if (!File.Exists(fullPath))
        {
            _logger.Log("    ... failed.");
            config = default;
            return false;
        }

        string json = File.ReadAllText(fullPath);

        config = JsonConvert.DeserializeObject<T>(json);
        
        _logger.Log("    ... loaded.");

        return config != null;
    }

    public void WriteConfig(string name, IConfig config)
    {
        string fullPath = Path.Combine(_configDirectory, $"{name}.json");

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        
        File.WriteAllText(fullPath, JsonConvert.SerializeObject(config, Formatting.Indented));
    }
}