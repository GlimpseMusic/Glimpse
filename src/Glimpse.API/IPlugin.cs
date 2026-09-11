using Glimpse.API.UI;

namespace Glimpse.API;

/// <summary>
/// The base interface for plugins.
/// </summary>
public interface IPlugin : IDisposable
{
    /// <summary>
    /// Gets if the plugin has been initialized.
    /// </summary>
    public bool IsInitialized { get; }

    /// <summary>
    /// Gets/sets the config associated with this plugin.
    /// This value may be accessed and updated by Glimpse.
    /// Return null if the plugin does not contain a config.
    /// </summary>
    /// <remarks>Glimpse will <b>NEVER</b> set a null config, so it is safe to assume the config is not null in the setter.</remarks>
    public IConfig? Config { get; set; }

    /// <summary>
    /// Called when the plugin is ready to be initialized.
    /// </summary>
    /// <param name="glimpse">The <see cref="IGlimpse"/> instance that is associated with this plugin.</param>
    public void Initialize(IGlimpse glimpse);

    /// <summary>
    /// Called when the plugin can display any extra settings on the settings page.
    /// </summary>
    /// <param name="ui">The immediate GUI instance.</param>
    public void OnGUI(IImmediateGUI ui) { }

    /// <summary>
    /// Called when the config should be saved to disk.
    /// </summary>
    public void SaveConfig() { }
}