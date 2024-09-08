using System.Runtime.InteropServices;

namespace Glimpse.Platforms;

public class WindowsPlatform : Platform
{
    public override void EnableDPIAwareness()
    {
        SetProcessDPIAware();
    }
    
    [DllImport("user32.dll")]
    private static extern bool SetProcessDPIAware();
}