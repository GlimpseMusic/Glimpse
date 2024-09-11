namespace Glimpse.Platforms;

public class LinuxPlatform : Platform
{
    // This shouldn't be necessary on Linux platforms.
    public override void EnableDPIAwareness() { }
    
    public override void EnableDarkWindow(nint hwnd) { }
}