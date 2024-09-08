using System;
using MixrSharp;

namespace Glimpse.Player.Codecs;

public abstract class CodecStream : IDisposable
{
    public abstract TrackInfo TrackInfo { get; }
    
    public abstract AudioFormat Format { get; }
    
    public abstract ulong LengthInSamples { get; }

    public abstract ulong GetBuffer(Span<byte> buffer);

    public abstract void Seek(ulong sample);
    
    public abstract void Dispose();
}