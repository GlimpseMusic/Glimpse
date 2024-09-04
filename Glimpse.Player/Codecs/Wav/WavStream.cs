using System;
using MixrSharp;

namespace Glimpse.Player.Codecs.Wav;

public class WavStream : CodecStream
{
    private readonly MixrSharp.Stream.Wav _wav;

    public override AudioFormat Format => _wav.Format;

    public override ulong LengthInSamples => _wav.LengthInSamples;

    public WavStream(string path)
    {
        _wav = new MixrSharp.Stream.Wav(path);
    }

    public override ulong GetBuffer(Span<byte> buffer)
        => _wav.GetBuffer(buffer);

    public override void Dispose()
    {
        _wav.Dispose();
    }
}