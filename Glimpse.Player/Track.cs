using System;
using System.Threading.Tasks;
using MixrSharp;
using MixrSharp.Stream;

namespace Glimpse.Player;

public class Track : IDisposable
{
    private AudioStream _stream;
    private AudioFormat _format;
    private AudioSource _source;

    private byte[] _audioBuffer;
    private AudioBuffer[] _buffers;
    private int _currentBuffer;

    private ulong _totalBytes;

    public readonly int LengthInSeconds;

    public int ElapsedSeconds
    {
        get
        {
            ulong totalSamples = _totalBytes / (ulong) _format.DataType.BytesPerSample() /
                                 (ulong) _format.Channels.AsInt();

            return (int) (totalSamples / _format.SampleRate);
        }
    }
    
    public TrackState State
    {
        get
        {
            return _source.State switch
            {
                SourceState.Stopped => TrackState.Stopped,
                SourceState.Paused => TrackState.Paused,
                SourceState.Playing => TrackState.Playing,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
    
    internal Track(Context context, AudioStream stream)
    {
        _stream = stream;

        _format = stream.Format;

        LengthInSeconds = (int) (_stream.PcmLengthInBytes / (ulong) _format.DataType.BytesPerSample() /
                                 (ulong) _format.Channels.AsInt() / _format.SampleRate);

        _source = context.CreateSource(new SourceDescription(SourceType.Pcm, _format));
        
        _audioBuffer = new byte[_format.SampleRate * _format.DataType.BytesPerSample()];

        _buffers = new AudioBuffer[2];
        for (int i = 0; i < _buffers.Length; i++)
        {
            _totalBytes += _stream.GetBuffer(_audioBuffer);
            _buffers[i] = context.CreateBuffer(_audioBuffer);
            _source.SubmitBuffer(_buffers[i]);
        }

        // The source will loop the last buffer if it runs out of buffers. It won't sound nice but at least it will
        // continue to play.
        _source.Looping = true;
        
        _source.BufferFinished += BufferFinished;
    }

    public void Play()
    {
        _source.Play();
    }

    public void Pause()
    {
        _source.Pause();
    }
    
    private void BufferFinished()
    {
        Task.Run(() =>
        {
            ulong bytesProcessed = _stream.GetBuffer(_audioBuffer);
            _totalBytes += bytesProcessed;

            if (bytesProcessed == 0)
            {
                // Disable looping so the source can successfully stop.
                _source.Looping = false;
                return;
            }

            _buffers[_currentBuffer].Update(_audioBuffer);
            _source.SubmitBuffer(_buffers[_currentBuffer]);

            _currentBuffer++;
            if (_currentBuffer >= _buffers.Length)
                _currentBuffer = 0;
        });
    }

    public void Dispose()
    {
        _source.Dispose();
        foreach (AudioBuffer buffer in _buffers)
            buffer.Dispose();
        _stream.Dispose();
    }
}