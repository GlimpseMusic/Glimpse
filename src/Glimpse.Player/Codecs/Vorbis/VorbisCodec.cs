namespace Glimpse.Player.Codecs.Vorbis;

public class VorbisCodec : Codec
{
    public override bool FileIsSupported(string path, string extension)
    {
        return extension == ".ogg";
    }

    public override CodecStream CreateStream(string path)
    {
        return new VorbisStream(path);
    }
}