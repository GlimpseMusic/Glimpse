using Glimpse.Player.Codecs;

namespace Glimpse.OpenMPT;

public class MptCodec : Codec
{
    public override bool FileIsSupported(string path, string extension)
    {
        return extension is ".it" or ".xm" or ".mod" or ".s3m" or ".mptm";
    }

    public override CodecStream CreateStream(string path)
    {
        return new MptStream(path);
    }
}