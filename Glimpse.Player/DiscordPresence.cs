using System;
using DiscordRPC;

namespace Glimpse.Player;

public static class DiscordPresence
{
    public static DiscordRpcClient Client;
    
    public static void Initialize()
    {
        Client = new DiscordRpcClient("1280266653950804111");
        if (!Client.Initialize())
            return;
    }

    public static void SetPresence(TrackInfo info, double songLengthInSeconds)
    {
        if (!Client.IsInitialized)
            return;

        RichPresence presence = new RichPresence()
            .WithDetails(info.Title)
            .WithState(info.Artist)
            .WithTimestamps(Timestamps.FromTimeSpan(songLengthInSeconds))
            .WithAssets(new Assets() { LargeImageText = info.Album/*, LargeImageKey = "https://coverartarchive.org/release/16a0757f-f02e-4dc9-a837-762776f3b565/38240718472.jpg"*/ });
        
        Client.SetPresence(presence);
    }

    public static void Deinitialize()
    {
        if (!Client.IsInitialized)
            return;
        
        Client.Dispose();
    }
}