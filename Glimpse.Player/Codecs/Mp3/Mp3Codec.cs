﻿using System.IO;

namespace Glimpse.Player.Codecs.Mp3;

public class Mp3Codec : Codec
{
    public override bool FileIsSupported(string path, string extension)
    {
        return extension == ".mp3";
    }

    public override CodecStream CreateStream(string path)
    {
        throw new System.NotImplementedException();
    }
}