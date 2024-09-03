using System;
using System.Numerics;
using Silk.NET.OpenGL;

namespace Glimpse.Graphics.GLUtils;

public class BufferShaderSet : IDisposable
{
    private readonly GL _gl;

    public readonly uint VertexBuffer;
    public readonly uint IndexBuffer;
    
    public readonly uint VertexArray;

    public readonly uint Program;

    public BufferShaderSet(GL gl, uint vertexBuffer, uint indexBuffer, string vertexShader, string fragmentShader)
    {
        _gl = gl;

        VertexBuffer = vertexBuffer;
        IndexBuffer = indexBuffer;
        
        VertexArray = _gl.CreateVertexArray();
        _gl.BindVertexArray(VertexArray);
        
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, vertexBuffer);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, indexBuffer);

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

    public static unsafe BufferShaderSet Create<TVertex, TIndex>(GL gl, in ReadOnlySpan<TVertex> vertices,
        in ReadOnlySpan<TIndex> indices, string vertexShader, string fragmentShader) 
        where TVertex : unmanaged
        where TIndex : unmanaged
    {
        // Unbind vertex array so we don't affect any that may be bound.
        gl.BindVertexArray(0);

        uint vertexBuffer = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vertexBuffer);

        fixed (void* pVertices = vertices)
        {
            gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint) (vertices.Length * sizeof(TVertex)), pVertices,
                BufferUsageARB.StaticDraw);
        }
        
        uint indexBuffer = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, indexBuffer);

        fixed (void* pIndices = indices)
        {
            gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint) (indices.Length * sizeof(TVertex)), pIndices,
                BufferUsageARB.StaticDraw);
        }

        return new BufferShaderSet(gl, vertexBuffer, indexBuffer, vertexShader, fragmentShader);
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