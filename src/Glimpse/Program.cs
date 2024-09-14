using System;
using System.IO;
using System.Runtime.InteropServices;
using Glimpse.Forms;
using Glimpse.Player;
using Glimpse.Player.Configs;
using Silk.NET.SDL;

namespace Glimpse;

public static class Program
{
    public static unsafe void Main(string[] args)
    {
#if !DEBUG
        try
#endif
        {
            Glimpse.Run(new GlimpsePlayer(), args);
        }
#if !DEBUG
        catch (Exception e)
        {
            Logger.Log(e.ToString());
            
            Sdl sdl = Sdl.GetApi();

            const string title = "Glimpse";
            
            string logLocation = Path.Combine(IConfig.BaseDir, "LastSession.log");
            string message = $"Oops! Glimpse crashed.\nLog file at: {logLocation}\n\nPlease send log file + the following error to the developers:\n{e}";

            sdl.ShowSimpleMessageBox((uint) MessageBoxFlags.Error, title, message, null);
        }
#endif
    }
}