using System;
using Glimpse.Api;
using Glimpse.Player;

namespace Glimpse.Platforms;

public class NullPlatform : Platform
{
    public override void InitializeMainWindow(IntPtr hwnd) { }

    public override void EnableDPIAwareness() { }

    public override void EnableDarkWindow(IntPtr hwnd) { }

    public override void OpenFileInExplorer(string path) { }

    public override void SetPlayState(TrackState state, TrackInfo info) { }
}