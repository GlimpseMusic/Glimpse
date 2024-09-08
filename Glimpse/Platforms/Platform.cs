using System;

namespace Glimpse.Platforms;

public abstract class Platform
{
    public abstract void EnableDPIAwareness();

    public static Platform AutoDetect()
    {
        if (OperatingSystem.IsWindows())
            return new WindowsPlatform();
        
        if (OperatingSystem.IsLinux())
            return new LinuxPlatform();
        
        throw new NotSupportedException();
    }
}