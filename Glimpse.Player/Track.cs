using System;
using MixrSharp;
using MixrSharp.Stream;

namespace Glimpse.Player;

public class Track : IDisposable
{
    private AudioStream _stream;
    private AudioSource _source;

    private byte[] _audioBuffer;
    
    internal Track(Context context, AudioStream stream)
    {
        _stream = stream;

        AudioFormat format = stream.Format;
    }

    public void Dispose()
    {
        _source.Dispose();
    }
}