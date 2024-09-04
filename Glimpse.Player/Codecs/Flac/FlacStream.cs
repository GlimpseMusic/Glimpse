using System;
using MixrSharp;

namespace Glimpse.Player.Codecs.Flac;

public class FlacStream : CodecStream
{
    private MixrSharp.Stream.Flac _flac;

    public override AudioFormat Format => _flac.Format;

    public override ulong LengthInSamples => _flac.LengthInSamples;

    public FlacStream(string path)
    {
        _flac = new MixrSharp.Stream.Flac(path);
    }

    public override ulong GetBuffer(Span<byte> buffer)
        => _flac.GetBuffer(buffer);

    public override void Dispose()
    {
        _flac.Dispose();
    }
}