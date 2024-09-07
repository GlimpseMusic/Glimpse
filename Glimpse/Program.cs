using System;
using Glimpse.Forms;
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

            //if (args.Length > 0)
            Glimpse.Run(new GlimpsePlayer(), null);
            //else
            //    throw new NotImplementedException();
        }
#if !DEBUG
        catch (Exception e)
        {
            Sdl sdl = Sdl.GetApi();

            const string title = "Glimpse";
            string message = $"Oops! Glimpse crashed. Please send the following error to the developers:\n{e}";

            sdl.ShowSimpleMessageBox((uint) MessageBoxFlags.Error, title, message, null);
        }
#endif
    }
}