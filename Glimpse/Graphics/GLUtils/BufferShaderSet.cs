using System;
using System.Numerics;
using Silk.NET.OpenGL;

namespace Glimpse.Graphics.GLUtils;

public unsafe class BufferShaderSet<TVertex, TIndex> : IDisposable 
    where TVertex : unmanaged
    where TIndex : unmanaged
{
    private readonly GL _gl;

    public uint VertexBuffer;
    public uint IndexBuffer;
    
    public readonly uint VertexArray;

    public readonly uint Program;

    public BufferShaderSet(GL gl, in ReadOnlySpan<TVertex> vertices, in ReadOnlySpan<TIndex> indices,
        string vertexShader, string fragmentShader, BufferUsageARB usage = BufferUsageARB.StaticDraw)
    {
        _gl = gl;
        
        VertexArray = _gl.CreateVertexArray();
        _gl.BindVertexArray(VertexArray);
        
        // Unbind vertex array so we don't affect any that may be bound.
        gl.BindVertexArray(0);

        VertexBuffer = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, VertexBuffer);

        fixed (void* pVertices = vertices)
            gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint) (vertices.Length * sizeof(TVertex)), pVertices, usage);
        
        IndexBuffer = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, IndexBuffer);

        fixed (void* pIndices = indices)
        {
            gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint) (indices.Length * sizeof(TIndex)), pIndices,
                usage);
        }

        uint vShader = CreateShader(gl, ShaderType.VertexShader, vertexShader);
        uint fShader = CreateShader(gl, ShaderType.FragmentShader, fragmentShader);

        Program = _gl.CreateProgram();
        
        _gl.AttachShader(Program, vShader);
        _gl.AttachShader(Program, fShader);
        
        _gl.LinkProgram(Program);

        if (gl.GetProgram(Program, ProgramPropertyARB.LinkStatus) != (int) GLEnum.True)
            throw new Exception($"Failed to link program: {_gl.GetProgramInfoLog(Program)}");
        
        _gl.DetachShader(Program, vShader);
        _gl.DetachShader(Program, fShader);
        _gl.DeleteShader(vShader);
        _gl.DeleteShader(fShader);
    }

    public BufferShaderSet(GL gl, uint numVertices, uint numIndices, string vertexShader, string fragmentShader,
        BufferUsageARB usage = BufferUsageARB.DynamicDraw) : this(gl,
        new ReadOnlySpan<TVertex>(null, (int) numVertices), new ReadOnlySpan<TIndex>(null, (int) numIndices),
        vertexShader, fragmentShader, usage) { }

    public void SetVector4(string name, Vector4 vector)
    {
        int location = _gl.GetUniformLocation(Program, name);
        _gl.Uniform4(location, vector);
    }

    public unsafe void SetMatrix4x4(string name, Matrix4x4 matrix)
    {
        int location = _gl.GetUniformLocation(Program, name);
        _gl.UniformMatrix4(location, 1, false, &matrix.M11);
    }

    public void Bind()
    {
        _gl.BindVertexArray(VertexArray);
        _gl.UseProgram(Program);
    }

    public void ResizeVertexBuffer(uint newSize, BufferUsageARB usage = BufferUsageARB.DynamicDraw)
    {
        _gl.DeleteBuffer(VertexBuffer);
        VertexBuffer = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, VertexBuffer);
        _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint) (newSize * sizeof(TVertex)), null, usage);
    }
    
    public void ResizeIndexBuffer(uint newSize, BufferUsageARB usage = BufferUsageARB.DynamicDraw)
    {
        _gl.DeleteBuffer(IndexBuffer);
        IndexBuffer = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, IndexBuffer);
        _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint) (newSize * sizeof(TIndex)), null, usage);
    }

    private static uint CreateShader(GL gl, ShaderType type, string source)
    {
        uint shader = gl.CreateShader(type);
        gl.ShaderSource(shader, source);
        
        gl.CompileShader(shader);

        if (gl.GetShader(shader, GLEnum.CompileStatus) != (int) GLEnum.True)
            throw new Exception($"Failed to compile {type}: {gl.GetShaderInfoLog(shader)}");

        return shader;
    }
    
    public void Dispose()
    {
        _gl.DeleteProgram(Program);
        
        _gl.DeleteVertexArray(VertexArray);
        
        _gl.DeleteBuffer(IndexBuffer);
        _gl.DeleteBuffer(VertexBuffer);
    }
}