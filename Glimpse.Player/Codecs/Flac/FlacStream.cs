using System;
using MixrSharp;

namespace Glimpse.Player.Codecs.Flac;

public class FlacStream : CodecStream
{
    private readonly MixrSharp.Stream.Flac _flac;

    public override TrackInfo TrackInfo { get; }
    
    public override AudioFormat Format => _flac.Format;

    public override ulong LengthInSamples => _flac.LengthInSamples;

    public FlacStream(string path)
    {
        _flac = new MixrSharp.Stream.Flac(path);
        TrackInfo = TrackInfo.FromFile(path);
    }

    public override ulong GetBuffer(Span<byte> buffer)
        => _flac.GetBuffer(buffer);

    public override void Dispose()
    {
        _flac.Dispose();
    }
}