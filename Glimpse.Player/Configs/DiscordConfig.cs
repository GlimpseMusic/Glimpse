using System.Collections.Generic;

namespace Glimpse.Player.Configs;

public class DiscordConfig : IConfig
{
    public Dictionary<string, string> AlbumArt;

    public DiscordConfig()
    {
        AlbumArt = new Dictionary<string, string>();
    }
}