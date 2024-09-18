namespace Glimpse.Api;

public abstract class Plugin : IDisposable
{
    public abstract void Initialize(IGlimpse glimpse);

    public abstract void Dispose();
}