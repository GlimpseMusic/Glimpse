﻿using System;
using Silk.NET.OpenGL;

namespace Glimpse.Graphics;

public class Image : IDisposable
{
    private readonly GL _gl;

    public readonly uint ID;

    public readonly uint Width;

    public readonly uint Height;
    
    internal unsafe Image(GL gl, byte[] data, uint width, uint height)
    {
        _gl = gl;

        Width = width;
        Height = height;

        ID = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, ID);

        fixed (byte* pData = data)
        {
            _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, width, height, 0, PixelFormat.Rgba,
                PixelType.UnsignedByte, pData);
        }
        
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.LinearMipmapLinear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);
        
        _gl.GenerateMipmap(TextureTarget.Texture2D);
    }

    public void Dispose()
    {
        _gl.DeleteTexture(ID);
    }
}