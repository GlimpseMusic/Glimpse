using Glimpse.Player.Configs;

namespace Glimpse.OpenMPT;

public class MptConfig : IConfig
{
    public bool EmulateAmigaResampler;

    public int ResamplerFilterMode;

    public bool FadeOutAtEnd;

    public MptConfig()
    {
        EmulateAmigaResampler = true;
        ResamplerFilterMode = 0;
        FadeOutAtEnd = false;
    }
}