namespace Glimpse.Player;

public struct PlayerSettings
{
    public uint SampleRate;

    public PlayerSettings(uint sampleRate)
    {
        SampleRate = sampleRate;
    }
}