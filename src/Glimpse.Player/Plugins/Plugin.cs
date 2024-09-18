using System;
using Glimpse.Api;

namespace Glimpse.Player.Plugins;

public abstract class Plugin : IDisposable
{
    public abstract void Initialize(IAudioPlayer player);

    public abstract void Dispose();
}