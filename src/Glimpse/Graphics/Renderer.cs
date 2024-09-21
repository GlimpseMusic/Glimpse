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
    private float _scale;
    
    private readonly Image _white;
    
    private readonly BufferShaderSet<Vertex2D, ushort> _imageRenderSet;

    private Matrix4x4 _projection;
    
    public readonly GL GL;

    public readonly ImGuiRenderer ImGui;
    
    public Renderer(GL gl, Size size, float scale)
    {
        GL = gl;
        _scale = scale;
        
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _white = CreateImage([255, 255, 255, 255], 1, 1);

        _projection = Matrix4x4.CreateOrthographicOffCenter(0, size.Width, size.Height, 0, -1, 1);

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

        _imageRenderSet = new BufferShaderSet<Vertex2D, ushort>(GL, vertices, indices, imageVertShader, imageFragShader);
        
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint) sizeof(Vertex2D), (void*) 0);
        
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, (uint) sizeof(Vertex2D), (void*) 8);
        
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, (uint) sizeof(Vertex2D), (void*) 16);

        ImGui = new ImGuiRenderer(GL, size, scale);
    }

    public Image CreateImage(byte[] data, uint width, uint height)
    {
        return new Image(GL, data, width, height);
    }

    public Image CreateImage(string path)
    {
        using FileStream stream = File.OpenRead(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));
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

    public void DrawImage(Image image, Vector2 position, Size size, Color tint)
    {
        _imageRenderSet.Bind();

        Matrix4x4 world = Matrix4x4.CreateScale(size.Width, size.Height, 1) *
                          Matrix4x4.CreateTranslation(position.X, position.Y, 0);

        _imageRenderSet.SetMatrix4x4("uTransform", world * _projection);
        
        _imageRenderSet.SetVector4("uTint", tint.Normalize());
        
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, image.ID);
        
        GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedShort, null);
    }

    public void DrawRectangle(Color color, Vector2 postion, Size size)
        => DrawImage(_white, postion, size, color);

    public void Resize(Size size)
    {
        GL.Viewport(0, 0, (uint) size.Width, (uint) size.Height);
        _projection = Matrix4x4.CreateOrthographicOffCenter(0, size.Width, size.Height, 0, -1, 1);
        
        ImGui.Resize(size);
    }
    
    public void Dispose()
    {
        _imageRenderSet.Dispose();
        _white.Dispose();
        GL.Dispose();
    }

    public const string ShaderAssemblyBase = "Glimpse.Graphics.Shaders.";
}