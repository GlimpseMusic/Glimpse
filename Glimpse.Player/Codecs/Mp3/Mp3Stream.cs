using System;
using MixrSharp;

namespace Glimpse.Player.Codecs.Mp3;

public class Mp3Stream : CodecStream
{
    private readonly MixrSharp.Stream.Mp3 _mp3;

    public override AudioFormat Format => _mp3.Format;

    public override ulong LengthInSamples => _mp3.LengthInSamples;

    public Mp3Stream(string path)
    {
        _mp3 = new MixrSharp.Stream.Mp3(path);
    }
    
    public override ulong GetBuffer(Span<byte> buffer)
        => _mp3.GetBuffer(buffer);

    public override void Dispose()
    {
        _mp3.Dispose();
    }
}