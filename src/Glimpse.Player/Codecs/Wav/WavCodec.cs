namespace Glimpse.Player.Codecs.Wav;

public class WavCodec : Codec
{
    public override bool FileIsSupported(string path, string extension)
    {
        return extension == ".wav";
    }

    public override CodecStream CreateStream(string path)
    {
        return new WavStream(path);
    }
}