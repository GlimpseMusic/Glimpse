using System.IO;

namespace Glimpse.Player.Codecs.Flac;

public class FlacCodec : Codec
{
    public override bool FileIsSupported(string path, string extension)
    {
        return extension == ".flac";
    }

    public override CodecStream CreateStream(string path)
    {
        return new FlacStream(path);
    }
}