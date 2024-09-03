using System;

namespace Glimpse.Player.Plugins;

public abstract class Plugin : IDisposable
{
    public abstract void Initialize(AudioPlayer player);

    public abstract void Dispose();
}