using Glimpse.Player.Codecs;

namespace Glimpse.OpenMPT;

public class MptCodec : Codec
{
    public override bool FileIsSupported(string path, string extension)
    {
        throw new NotImplementedException();
    }

    public override CodecStream CreateStream(string path)
    {
        throw new NotImplementedException();
    }
}