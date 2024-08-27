namespace Glimpse.Player;

public struct PlayerSettings
{
    public uint SampleRate;

    public float Volume;
    
    public double SpeedAdjust;

    public PlayerSettings(uint sampleRate)
    {
        SampleRate = sampleRate;
        Volume = 1.0f;
        SpeedAdjust = 1.0;
    }
}