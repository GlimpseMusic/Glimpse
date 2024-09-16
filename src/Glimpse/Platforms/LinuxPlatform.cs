using System;
using Glimpse.Player;

namespace Glimpse.Platforms;

public class LinuxPlatform : Platform
{
    // This shouldn't be necessary on Linux platforms.
    public override void InitializeMainWindow(IntPtr hwnd) { }

    public override void EnableDPIAwareness() { }
    
    public override void EnableDarkWindow(nint hwnd) { }
    
    public override void OpenFileInExplorer(string path) { }

    public override void SetPlayState(TrackState state, TrackInfo info) { }
}