using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Glimpse.API;
using Glimpse.Configs;
using Glimpse.Library;

namespace Glimpse;

public class ConfigManager : IConfigManager
{
    private readonly Logger _logger;
    
    public ConfigManager(Logger logger)
    {
        _logger = logger;
    }

    public bool TryGetConfig<T>(string name, out T config) where T : IConfig
    {
        string fullPath = Path.Combine(IConfigManager.BaseDir, $"{name}.json");
        _logger.Log($"Trying to load config {fullPath}.");

        if (!File.Exists(fullPath))
        {
            _logger.Log("    ... failed.");
            config = default;
            return false;
        }

        string json = File.ReadAllText(fullPath);

        config = JsonSerializer.Deserialize<T>(json, GetDefaultSerializerOptions(Assembly.GetCallingAssembly() != Assembly.GetExecutingAssembly()));
        
        _logger.Log("    ... loaded.");

        return config != null;
    }

    public void WriteConfig<T>(string name, T config) where T : IConfig
    {
        string fullPath = Path.Combine(IConfigManager.BaseDir, $"{name}.json");
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        File.WriteAllText(fullPath, JsonSerializer.Serialize(config, GetDefaultSerializerOptions(Assembly.GetCallingAssembly() != Assembly.GetExecutingAssembly())));
    }

    public static JsonSerializerOptions GetDefaultSerializerOptions(bool useReflection = false)
    {
        JsonSerializerOptions options = new JsonSerializerOptions()
        {
            IncludeFields = true,
            WriteIndented = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

#if PUBLISH_AOT
        options.TypeInfoResolver = new DefaultJsonTypeInfoResolver();
#else
        if (useReflection || JsonSerializer.IsReflectionEnabledByDefault)
            options.TypeInfoResolver = new DefaultJsonTypeInfoResolver();
        else
            options.TypeInfoResolver = ConfigSerializerContext.Default;
#endif


        return options;
    }
}

[JsonSourceGenerationOptions(WriteIndented = true, IncludeFields = true, ReadCommentHandling = JsonCommentHandling.Skip)]
[JsonSerializable(typeof(GlimpseConfig))]
[JsonSerializable(typeof(Locale))]
[JsonSerializable(typeof(Locale.AvailableLocale))]
[JsonSerializable(typeof(Locale.LocaleSet))]
[JsonSerializable(typeof(MusicLibrary))]
[JsonSerializable(typeof(Library.Library))]
[JsonSerializable(typeof(Theme))]
internal partial class ConfigSerializerContext : JsonSerializerContext;