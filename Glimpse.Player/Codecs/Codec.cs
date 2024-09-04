namespace Glimpse.Player.Codecs;

public abstract class Codec
{
    public abstract bool FileIsSupported(string path, string extension);

    public abstract CodecStream CreateStream(string path);
}