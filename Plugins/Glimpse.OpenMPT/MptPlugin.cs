using Glimpse.Player;
using Glimpse.Player.Plugins;

namespace Glimpse.OpenMPT;

public class MptPlugin : Plugin
{
    public override void Initialize(AudioPlayer player)
    {
        player.Codecs.Add(new MptCodec());
    }

    public override void Dispose()
    {
        
    }
}