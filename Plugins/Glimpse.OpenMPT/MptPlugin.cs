using Glimpse.Player;
using Glimpse.Player.Configs;
using Glimpse.Player.Plugins;

namespace Glimpse.OpenMPT;

public class MptPlugin : Plugin
{
    public MptConfig Config;
    
    public override void Initialize(AudioPlayer player)
    {
        if (!IConfig.TryGetConfig("MPT", out MptConfig Config))
        {
            Config = new MptConfig();
            IConfig.WriteConfig("MPT", Config);
        }
        
        player.Codecs.Add(new MptCodec(Config));
    }

    public override void Dispose()
    {
        
    }
}