namespace Glimpse.Player.Configs;

public class PlayerConfig : IConfig
{
    public uint SampleRate;

    public float Volume;
    
    public double SpeedAdjust;

    public bool AutoPlay;

    public PlayerConfig()
    {
        SampleRate = 48000;
        Volume = 1.0f;
        SpeedAdjust = 1.0;
        AutoPlay = true;
    }
}