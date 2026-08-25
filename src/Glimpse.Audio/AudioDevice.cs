using System.Runtime.InteropServices;
using System.Timers;
using MixrSharp;
using piko.SDL3;
using Timer = System.Timers.Timer;

namespace Glimpse.Audio;

public sealed unsafe class AudioDevice : IDisposable
{
    private readonly Context _context;
    private readonly uint _sampleRate;
    private readonly SDL.AudioStreamCallback _callback;

    private SDL.AudioStream _device;
    private readonly Timer _stopTimer;

    public AudioDevice(Context context, uint sampleRate)
    {
        _context = context;
        _sampleRate = sampleRate;
        
        if (!SDL.Init(SDL.InitFlags.Audio))
            throw new Exception($"Failed to initialize SDL: {SDL.GetError()}");

        _callback = AudioCallback;

        // stops the audio device after 10 seconds of inactivity when running
        _stopTimer = new Timer(10000)
        {
            AutoReset = false,
            Enabled = false
        };
        _stopTimer.Elapsed += StopDeviceCallback;
    }

    /// <summary>
    /// Start the device playback. You must call this before playback will begin!
    /// </summary>
    public void Play()
    {
        // cancel the stop timer if it's running
        _stopTimer.Stop();

        // don't create a new device if one already exists.
        if (!_device.IsNull)
            return;

        SDL.AudioSpec spec = new()
        {
            Freq = (int) _sampleRate,
            Format = SDL.AudioFormat.F32le,
            Channels = 2
        };

        _device = SDL.OpenAudioDeviceStream(SDL.AudioDeviceDefaultPlayback, &spec, _callback, 0);
        if (_device.IsNull)
            throw new Exception($"Failed to open audio device: {SDL.GetError()}");

        SDL.ResumeAudioStreamDevice(_device);
    }

    /// <summary>
    /// Stop the device playback. You should call this once the device is no longer needed, for example when stopped,
    /// or after a long enough pause.
    /// </summary>
    public void Pause()
    {
        // start the stop timer that stops the startings
        _stopTimer.Start();
    }

    private void StopDeviceCallback(object? sender, ElapsedEventArgs elapsedEventArgs)
    {
        Console.WriteLine("Stop!");
        // don't try to destroy a nonexistent device. in theory this is unreachable
        if (_device.IsNull)
            return;

        SDL.PauseAudioStreamDevice(_device);
        SDL.DestroyAudioStream(_device);
        _device = new SDL.AudioStream(); // todo piko: IHandle.Null static?
    }
    
    public void Dispose()
    {
        _stopTimer.Stop();
        _stopTimer.Elapsed -= StopDeviceCallback;
        SDL.DestroyAudioStream(_device);
        SDL.QuitSubSystem(SDL.InitFlags.Audio);
    }
    
    private void AudioCallback(nint userData, SDL.AudioStream stream, int additionalAmount, int totalAmount)
    {
        const int bufferSize = 512;
        float* buffer = stackalloc float[bufferSize];
        while (additionalAmount > 0)
        {
            int total = int.Min(additionalAmount, bufferSize);
            Span<float> bufferSlice = new Span<float>(buffer, total / 4);
            _context.MixToStereoF32Buffer(bufferSlice);
            SDL.PutAudioStreamData(stream, (nint) buffer, total);
            additionalAmount -= total;
        }
    }
}