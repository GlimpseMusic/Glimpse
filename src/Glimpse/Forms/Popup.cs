using System;
using System.Numerics;
using Glimpse.Graphics;

namespace Glimpse.Forms;

public abstract class Popup : IDisposable
{
    public bool IsRemoved;

    public Renderer Renderer;

    public float Scale;
    
    public abstract void Update();

    public void Close()
    {
        IsRemoved = true;
    }
    
    protected Vector2 ScaleVec(float x, float y)
    {
        float scale = Scale;
        return new Vector2((int) (x * scale), (int) (y * scale));
    }

    protected Vector2 ScaleVec(float scalar)
        => ScaleVec(scalar, scalar);

    public virtual void Dispose() { }
}