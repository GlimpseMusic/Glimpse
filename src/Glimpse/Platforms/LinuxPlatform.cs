using System;
using System.Diagnostics;
using System.IO;
using Glimpse.Player;

namespace Glimpse.Platforms;

public class LinuxPlatform : Platform
{
    public readonly string DefaultFileManager;
    
    public LinuxPlatform()
    {
        using Process process = new Process()
        {
            StartInfo = new ProcessStartInfo("xdg-mime")
            {
                Arguments = "query default inode/directory",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        process.WaitForExit();
        string fileManager = process.StandardOutput.ReadLine();

        if (fileManager == null)
            return;

        fileManager = fileManager.ToLower();

        if (fileManager.Contains("nautilus"))
            DefaultFileManager = "nautilus";
        else if (fileManager.Contains("dolphin"))
            DefaultFileManager = "dolphin";
    }
    
    // This shouldn't be necessary on Linux platforms.
    public override void InitializeMainWindow(IntPtr hwnd) { }

    public override void EnableDPIAwareness() { }
    
    public override void EnableDarkWindow(nint hwnd) { }

    public override void OpenFileInExplorer(string path)
    {
        Process process;
        
        if (DefaultFileManager == null)
        {
            process = new Process()
            {
                StartInfo = new ProcessStartInfo("xdg-open")
                {
                    Arguments = $"\"{Path.GetDirectoryName(path)}\""
                }
            };
        }
        else
        {
            process = new Process()
            {
                StartInfo = new ProcessStartInfo(DefaultFileManager)
                {
                    Arguments = $"--select \"{path}\""
                }
            };
        }

        process.Start();
        process.Dispose();
    }

    public override void SetPlayState(TrackState state, TrackInfo info) { }
}