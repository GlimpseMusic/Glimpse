using System;
using Silk.NET.OpenGL;

namespace Glimpse.Graphics;

public class Image : IDisposable
{
    private readonly GL _gl;

    internal readonly uint Texture;
    
    internal unsafe Image(GL gl, byte[] data, uint width, uint height)
    {
        _gl = gl;

        Texture = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, Texture);

        fixed (byte* pData = data)
        {
            _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb, width, height, 0, PixelFormat.Rgba,
                PixelType.UnsignedByte, pData);
        }
        
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.LinearMipmapLinear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);
        
        _gl.GenerateMipmap(TextureTarget.Texture2D);
    }

    public void Dispose()
    {
        _gl.DeleteTexture(Texture);
    }
}