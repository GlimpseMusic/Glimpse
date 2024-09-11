using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

namespace Glimpse.Platforms;

public class WindowsPlatform : Platform
{
    public override void EnableDPIAwareness()
    {
        SetProcessDPIAware();
    }

    public override unsafe void EnableDarkWindow(nint hwnd)
    {
        BOOL value = true;
        Windows.DwmSetWindowAttribute((HWND) hwnd, 20, &value, (uint) sizeof(BOOL));
    }

    [DllImport("user32.dll")]
    private static extern bool SetProcessDPIAware();
}