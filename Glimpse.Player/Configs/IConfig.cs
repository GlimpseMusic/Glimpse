using System;
using System.IO;
using Newtonsoft.Json;

namespace Glimpse.Player.Configs;

public interface IConfig
{
    public static string BaseDir
    {
        get
        {
#if DEBUG
            return "Config";
#else
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Glimpse");
#endif
        }
    }

    public static bool TryGetConfig<T>(string name, out T config) where T : IConfig
    {
        string fullPath = Path.Combine(BaseDir, $"{name}.json");

        if (!File.Exists(fullPath))
        {
            config = default;
            return false;
        }

        string json = File.ReadAllText(fullPath);

        config = JsonConvert.DeserializeObject<T>(json);
        
        Console.WriteLine($"Config {fullPath} loaded.");

        return config != null;
    }

    public static void WriteConfig(string name, IConfig config)
    {
        string fullPath = Path.Combine(BaseDir, $"{name}.json");

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        
        File.WriteAllText(fullPath, JsonConvert.SerializeObject(config, Formatting.Indented));
    }
}