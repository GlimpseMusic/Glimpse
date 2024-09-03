using System;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Reflection;
using Glimpse.Graphics.GLUtils;
using Silk.NET.OpenGL;
using StbImageSharp;

namespace Glimpse.Graphics;

public unsafe class Renderer : IDisposable
{
    private readonly BufferShaderSet _imageRenderSet;

    private Matrix4x4 _projection;
    
    public readonly GL GL;
    
    public Renderer(GL gl, uint width, uint height)
    {
        GL = gl;

        _projection = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1);

        ReadOnlySpan<Vertex2D> vertices = stackalloc Vertex2D[]
        {
            new Vertex2D(new Vector2(0, 0), new Vector2(0, 0), Vector4.One),
            new Vertex2D(new Vector2(1, 0), new Vector2(1, 0), Vector4.One),
            new Vertex2D(new Vector2(1, 1), new Vector2(1, 1), Vector4.One),
            new Vertex2D(new Vector2(0, 1), new Vector2(0, 1), Vector4.One)
        };

        ReadOnlySpan<ushort> indices = stackalloc ushort[]
        {
            0, 1, 3,
            1, 2, 3
        };
        
        string imageVertShader = Resource.LoadString(Assembly.GetExecutingAssembly(), ShaderAssemblyBase + "Image.vert");
        string imageFragShader = Resource.LoadString(Assembly.GetExecutingAssembly(), ShaderAssemblyBase + "Image.frag");

        _imageRenderSet = BufferShaderSet.Create(GL, vertices, indices, imageVertShader, imageFragShader);
        
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint) sizeof(Vertex2D), (void*) 0);
        
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, (uint) sizeof(Vertex2D), (void*) 8);
        
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, (uint) sizeof(Vertex2D), (void*) 16);
    }

    public Image CreateImage(byte[] data, uint width, uint height)
    {
        return new Image(GL, data, width, height);
    }

    public Image CreateImage(string path)
    {
        using FileStream stream = File.OpenRead(path);
        ImageResult result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        return new Image(GL, result.Data, (uint) result.Width, (uint) result.Height);
    }

    public Image CreateImage(byte[] data)
    {
        ImageResult result = ImageResult.FromMemory(data, ColorComponents.RedGreenBlueAlpha);
        return new Image(GL, result.Data, (uint) result.Width, (uint) result.Height);
    }

    public void Clear(Color color)
    {
        GL.ClearColor(color);
        GL.Clear(ClearBufferMask.ColorBufferBit);
    }

    public void DrawImage(Image image, Vector2 position, Size size)
    {
        _imageRenderSet.Bind();

        Matrix4x4 world = Matrix4x4.CreateScale(size.Width, size.Height, 1) *
                          Matrix4x4.CreateTranslation(position.X, position.Y, 0);

        _imageRenderSet.SetMatrix4x4("uTransform", world * _projection);
        
        _imageRenderSet.SetVector4("uTint", Vector4.One);
        
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, image.Texture);
        
        GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedShort, null);
    }
    
    public void Dispose()
    {
        GL.Dispose();
    }

    public const string ShaderAssemblyBase = "Glimpse.Graphics.Shaders.";
}