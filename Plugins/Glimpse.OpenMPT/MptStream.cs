using System.Runtime.CompilerServices;
using Glimpse.Player;
using Glimpse.Player.Codecs;
using MixrSharp;
using OpenMPT.NET;

namespace Glimpse.OpenMPT;

public class MptStream : CodecStream
{
    private readonly Module _module;

    private int _position;

    public override TrackInfo TrackInfo { get; }

    public override AudioFormat Format =>
        new AudioFormat(DataType.F32, (uint) _module.SampleRate, (byte) _module.Channels);

    public override ulong LengthInSamples => (ulong) (_module.DurationInSeconds * _module.SampleRate);

    public MptStream(string path)
    {
        _module = Module.FromMemory(File.ReadAllBytes(path), new ModuleOptions(emulateAmigaResampler: true));

        ModuleMetadata metadata = _module.Metadata;
        TrackInfo = new TrackInfo(metadata.Title ?? TrackInfo.UnknownTitle, metadata.Artist ?? TrackInfo.UnknownArtist,
            TrackInfo.UnknownAlbum, null);
    }
    
    public override unsafe ulong GetBuffer(Span<byte> buffer)
    {
        ulong totalBytes = 0;

        while (totalBytes < (ulong) buffer.Length)
        {
            uint samples = (uint) _module.AdvanceBuffer();

            if (samples == 0)
                break;

            uint copyAmount = (uint) (samples * _module.Channels * sizeof(float));
            if (totalBytes + copyAmount >= (ulong) buffer.Length)
                copyAmount = (uint) (buffer.Length - (int) totalBytes);
            
            fixed (byte* pBuffer = buffer)
            fixed (float* pModuleBuffer = _module.Buffer)
                Unsafe.CopyBlock(pBuffer + totalBytes, pModuleBuffer, copyAmount);

            totalBytes += copyAmount;
        }

        return totalBytes;
    }

    public override void Dispose()
    {
        _module.Dispose();
    }
}