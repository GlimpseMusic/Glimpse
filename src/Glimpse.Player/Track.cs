using System;
using System.Threading.Tasks;
using Glimpse.Player.Codecs;
using Glimpse.Player.Configs;
using MixrSharp;
using MixrSharp.Stream;

namespace Glimpse.Player;

public class Track : IDisposable
{
    private CodecStream _stream;
    private AudioFormat _format;
    private AudioSource _source;

    private byte[] _audioBuffer;
    private AudioBuffer[] _buffers;
    private int _currentBuffer;

    private ulong _totalBytes;

    public readonly TrackInfo Info;
    
    public readonly int LengthInSeconds;

    public int ElapsedSeconds
    {
        get
        {
            ulong totalSamples = _totalBytes / _format.BytesPerSample / _format.Channels;
            totalSamples += _source.Position;

            // TODO: Make this better.
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
    
    internal Track(Context context, CodecStream stream, TrackInfo info, PlayerConfig config)
    {
        _stream = stream;

        _format = stream.Format;

        Info = info;

        LengthInSeconds = (int) (_stream.LengthInSamples / _format.SampleRate);
        Logger.Log($"LengthInSeconds: {LengthInSeconds}");

        Logger.Log("Creating source.");
        _source = context.CreateSource(new SourceDescription(SourceType.Pcm, _format));
        
        _audioBuffer = new byte[_format.SampleRate * _format.Channels * _format.BytesPerSample];

        Logger.Log("Creating audio buffers.");
        _buffers = new AudioBuffer[2];
        for (int i = 0; i < _buffers.Length; i++)
        {
            _stream.GetBuffer(_audioBuffer);
            _buffers[i] = context.CreateBuffer(_audioBuffer);
            _source.SubmitBuffer(_buffers[i]);
        }

        // The source will loop the last buffer if it runs out of buffers. It won't sound nice but at least it will
        // continue to play.
        _source.Looping = true;
        
        _source.Speed = config.SpeedAdjust;
        
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

    public void Seek(int second)
    {
        _source.Pause();
        _stream.Seek((ulong) (second * _format.SampleRate));
        _source.ClearBuffers();
        _currentBuffer = 0;
        for (int i = 0; i < _buffers.Length; i++)
        {
            _stream.GetBuffer(_audioBuffer);
            _buffers[i].Update(_audioBuffer);
            _source.SubmitBuffer(_buffers[i]);
        }
        _source.Play();

        _totalBytes = (ulong) (second * _format.SampleRate * _format.Channels * _format.BytesPerSample);
    }
    
    private void BufferFinished()
    {
        _totalBytes += (ulong) _audioBuffer.Length;
        
        Task.Run(() =>
        {
            ulong bytesProcessed = _stream.GetBuffer(_audioBuffer);

            if (bytesProcessed == 0)
            {
                // Disable looping so the source can successfully stop.
                _source.Looping = false;
                return;
            }

            if ((int) bytesProcessed < _audioBuffer.Length)
                _buffers[_currentBuffer].Update(_audioBuffer[..(int) bytesProcessed]);
            else
                _buffers[_currentBuffer].Update(_audioBuffer);
            _source.SubmitBuffer(_buffers[_currentBuffer]);

            _currentBuffer++;
            if (_currentBuffer >= _buffers.Length)
                _currentBuffer = 0;
        });
    }

    public void Dispose()
    {
        Logger.Log("Disposing source.");
        _source.Dispose();
        Logger.Log("Disposing buffers.");
        foreach (AudioBuffer buffer in _buffers)
            buffer.Dispose();
        Logger.Log("Disposing stream.");
        _stream.Dispose();
    }
}