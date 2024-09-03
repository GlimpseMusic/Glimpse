using System;
using System.Drawing;
using Silk.NET.OpenGL;

namespace Glimpse.Graphics;

public class Renderer : IDisposable
{
    private uint _imageVertexBuffer;
    private uint _imageIndexBuffer;
    
    public readonly GL GL;
    
    public Renderer(GL gl)
    {
        GL = gl;
    }

    public void Clear(Color color)
    {
        GL.ClearColor(color);
        GL.Clear(ClearBufferMask.ColorBufferBit);
    }
    
    public void Dispose()
    {
        GL.Dispose();
    }
}