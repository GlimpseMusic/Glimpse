using System;
using Glimpse.Player;

namespace Glimpse.Platforms;

public abstract class Platform
{
    public event OnButtonPressed ButtonPressed = delegate { };

    public abstract void InitializeMainWindow(nint hwnd);
    
    public abstract void EnableDPIAwareness();

    public abstract void EnableDarkWindow(nint hwnd);

    public abstract void OpenFileInExplorer(string path);

    public abstract void SetPlayState(TrackState state, TrackInfo info);

    protected void InvokeButtonPressed(TransportButton button)
    {
        ButtonPressed(button);
    }

    public static Platform AutoDetect()
    {
        if (OperatingSystem.IsWindows())
            return new WindowsPlatform();
        
        if (OperatingSystem.IsLinux())
            return new LinuxPlatform();

        return new NullPlatform();
    }

    public delegate void OnButtonPressed(TransportButton button);
}