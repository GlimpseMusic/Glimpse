using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Glimpse.Graphics.GLUtils;
using Hexa.NET.ImGui;
using Silk.NET.OpenGL;
using ImDrawIdx = ushort;

namespace Glimpse.Graphics;

public class ImGuiRenderer : IDisposable
{
    private GL _gl;
    private Size _size;
    
    private readonly ImGuiContextPtr _context;
    
    private uint _vBufferSize;
    private uint _iBufferSize;

    private BufferShaderSet<ImDrawVert, ImDrawIdx> _bufferSet;

    private uint _imGuiTexture;

    public readonly Dictionary<string, ImFontPtr> Fonts;

    public ImGuiContextPtr ImGuiContext => _context;
    
    public unsafe ImGuiRenderer(GL gl, Size size)
    {
        _gl = gl;
        _size = size;
        
        _context = ImGui.CreateContext();
        ImGui.SetCurrentContext(_context);
        
        _vBufferSize = 5000;
        _iBufferSize = 10000;
        
        string vertexShader = Resource.LoadString(Assembly.GetExecutingAssembly(), Renderer.ShaderAssemblyBase + "ImGui.vert");
        string fragmentShader = Resource.LoadString(Assembly.GetExecutingAssembly(), Renderer.ShaderAssemblyBase + "ImGui.frag");

        _bufferSet =
            new BufferShaderSet<ImDrawVert, ImDrawIdx>(gl, _vBufferSize, _iBufferSize, vertexShader, fragmentShader);
        
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint) sizeof(ImDrawVert), (void*) 0);
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, (uint) sizeof(ImDrawVert), (void*) 8);
        _gl.EnableVertexAttribArray(2);
        _gl.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, (uint) sizeof(ImDrawVert), (void*) 16);

        ImGuiIOPtr io = ImGui.GetIO();
        io.DisplaySize = new Vector2(size.Width, size.Height);
        io.IniFilename = null;
        io.LogFilename = null;
        
        io.Fonts.AddFontDefault();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.FontGlobalScale = 1.0f;
        
        RecreateFontTexture();

        Fonts = new Dictionary<string, ImFontPtr>();
    }

    public ImFontPtr AddFont(string path, uint size, string name)
    {
        ImFontPtr font = ImGui.GetIO().Fonts.AddFontFromFileTTF(path, size);
        Fonts.Add(name, font);
        RecreateFontTexture();

        return font;
    }

    public unsafe void SetDefaultFont(ImFontPtr font)
    {
        ImGui.GetIO().FontDefault = font;
    }

    internal unsafe void Render()
    {
        ImGui.SetCurrentContext(_context);
        
        _gl.Enable(EnableCap.ScissorTest);
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        
        ImGui.Render();
        ImDrawDataPtr drawData = ImGui.GetDrawData();

        if (drawData.TotalVtxCount >= _vBufferSize)
        {
            Console.WriteLine("Recreate vertex buffer.");
            _vBufferSize = (uint) (drawData.TotalVtxCount + 5000);
            _bufferSet.ResizeVertexBuffer(_vBufferSize);
        }

        if (drawData.TotalIdxCount >= _iBufferSize)
        {
            Console.WriteLine("Recreate index buffer.");
            _iBufferSize = (uint) (drawData.TotalIdxCount + 10000);
            _bufferSet.ResizeIndexBuffer(_iBufferSize);
        }

        uint vertexOffset = 0;
        uint indexOffset = 0;
        
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _bufferSet.VertexBuffer);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _bufferSet.IndexBuffer);
        void* vPtr = _gl.MapBuffer(BufferTargetARB.ArrayBuffer, BufferAccessARB.WriteOnly);
        void* iPtr = _gl.MapBuffer(BufferTargetARB.ElementArrayBuffer, BufferAccessARB.WriteOnly);
        for (int i = 0; i < drawData.CmdListsCount; i++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[i];
            
            Unsafe.CopyBlock((byte*) vPtr + vertexOffset, (void*) cmdList.VtxBuffer.Data, (uint) (cmdList.VtxBuffer.Size * sizeof(ImDrawVert)));
            Unsafe.CopyBlock((byte*) iPtr + indexOffset, (void*) cmdList.IdxBuffer.Data, (uint) (cmdList.IdxBuffer.Size * sizeof(ImDrawIdx)));

            vertexOffset += (uint) (cmdList.VtxBuffer.Size * sizeof(ImDrawVert));
            indexOffset += (uint) (cmdList.IdxBuffer.Size * sizeof(ImDrawIdx));
        }
        _gl.UnmapBuffer(BufferTargetARB.ArrayBuffer);
        _gl.UnmapBuffer(BufferTargetARB.ElementArrayBuffer);

        _bufferSet.SetMatrix4x4("uProjection",
            Matrix4x4.CreateOrthographicOffCenter(drawData.DisplayPos.X, drawData.DisplayPos.X + drawData.DisplaySize.X,
                drawData.DisplayPos.Y + drawData.DisplaySize.Y, drawData.DisplayPos.Y, -1, 1));

        _gl.Viewport(0, 0, (uint) drawData.DisplaySize.X, (uint) drawData.DisplaySize.Y);
        
        _bufferSet.Bind();

        vertexOffset = 0;
        indexOffset = 0;
        Vector2 clipOff = drawData.DisplayPos;
        for (int i = 0; i < drawData.CmdListsCount; i++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[i];

            for (int j = 0; j < cmdList.CmdBuffer.Size; j++)
            {
                ImDrawCmd drawCmd = cmdList.CmdBuffer[j];
                
                if (drawCmd.UserCallback != null)
                    continue;
                
                _gl.ActiveTexture(TextureUnit.Texture0);
                _gl.BindTexture(TextureTarget.Texture2D, (uint) drawCmd.TextureId.Handle);

                Vector2 clipMin = new Vector2(drawCmd.ClipRect.X - clipOff.X, drawCmd.ClipRect.Y - clipOff.Y);
                Vector2 clipMax = new Vector2(drawCmd.ClipRect.Z - clipOff.X, drawCmd.ClipRect.W - clipOff.Y);
                
                if (clipMax.X <= clipMin.X || clipMax.Y <= clipMin.Y)
                    continue;

                _gl.Scissor((int) clipMin.X, (int) (drawData.DisplaySize.Y - clipMax.Y), (uint) (clipMax.X - clipMin.X),
                    (uint) (clipMax.Y - clipMin.Y));

                _gl.DrawElementsBaseVertex(PrimitiveType.Triangles, drawCmd.ElemCount, DrawElementsType.UnsignedShort,
                    (void*) ((drawCmd.IdxOffset + indexOffset) * sizeof(ImDrawIdx)),
                    (int) (drawCmd.VtxOffset + vertexOffset));
            }

            vertexOffset += (uint) cmdList.VtxBuffer.Size;
            indexOffset += (uint) cmdList.IdxBuffer.Size;
        }
    }

    internal void Resize(in Size size)
    {
        _size = size;
        ImGui.GetIO().DisplaySize = new Vector2(size.Width, size.Height);
    }
    
    private unsafe void RecreateFontTexture()
    {
        if (_gl.IsTexture(_imGuiTexture))
            _gl.DeleteTexture(_imGuiTexture);

        ImGuiIOPtr io = ImGui.GetIO();
        byte* pixels;
        int width, height;
        io.Fonts.GetTexDataAsRGBA32(&pixels, &width, &height);

        _imGuiTexture = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, _imGuiTexture);
        _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint) width, (uint) height, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
        
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);

        //_gl.GenerateMipmap(TextureTarget.Texture2D);
        
        io.Fonts.SetTexID((IntPtr) _imGuiTexture);
    }
    
    public void Dispose()
    {
        ImGui.DestroyContext(_context);
    }
}