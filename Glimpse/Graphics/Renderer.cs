using System;
using Silk.NET.OpenGL;

namespace Glimpse.Graphics;

public class Renderer : IDisposable
{
    public readonly GL GL;
    
    public Renderer(GL gl)
    {
        GL = gl;
    }
    
    public void Dispose()
    {
        
    }
}