using System;
using Glimpse.Graphics;

namespace Glimpse.Forms;

public abstract class Popup : IDisposable
{
    public bool IsRemoved;

    public Renderer Renderer;
    
    public abstract void Update();

    public void Close()
    {
        IsRemoved = true;
    }

    public virtual void Dispose() { }
}