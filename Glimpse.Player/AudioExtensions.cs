using System;
using MixrSharp;

namespace Glimpse.Player;

public static class AudioExtensions
{
    public static int BitsPerSample(this DataType type)
    {
        return type switch
        {
            DataType.I8 => 8,
            DataType.U8 => 8,
            DataType.I16 => 16,
            DataType.I32 => 32,
            DataType.F32 => 32,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static int BytesPerSample(this DataType type)
        => type.BitsPerSample() / 8;

    public static int AsInt(this Channels channels)
    {
        return channels switch
        {
            Channels.Mono => 1,
            Channels.Stereo => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(channels), channels, null)
        };
    }
}