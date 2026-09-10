using System.IO.Pipes;
using System.Net.Sockets;
using Glimpse.API;
using Glimpse.Forms;
using piko.SDL3;

namespace Glimpse;

public static class Program
{
    [STAThread]
    public static unsafe void Main(string[] args)
    {
#if !DEBUG
        Console.WriteLine("Checking for an already existing instance.");
        NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", Glimpse.PipeServerName, PipeDirection.Out);
        try
        {
            pipeClient.Connect(100);
            Console.WriteLine("Found!");
            if (args.Length > 0)
            {
                string cwd = Environment.CurrentDirectory;
                
                BinaryWriter writer = new BinaryWriter(pipeClient);
                foreach (string path in args)
                {
                    writer.Write((byte) CommunicationFlags.PlayFile);
                    
                    // Ensure the absolute path is always written otherwise the existing Glimpse instance won't know
                    // what to do with it.
                    if (Path.IsPathRooted(path))
                        writer.Write(path);
                    else
                        writer.Write(Path.Combine(cwd, path));
                }
            }

            return;
        }
        catch (TimeoutException) { }
        catch (SocketException) { }

        pipeClient.Dispose();
        
        AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
        {
            Logger logger = new Logger();
            string msg = eventArgs.ExceptionObject?.ToString() ??
                         "An error occurred, but there was no message attached.";
            logger.Log(msg);
            SDL.ShowSimpleMessageBox(SDL.MessageBoxFlags.Error, "Glimpse", msg, new SDL.Window());
        };

        try
#endif
        {
            using Glimpse glimpse = new Glimpse();
            glimpse.Run(new GlimpsePlayer(), args);
        }
#if !DEBUG
        catch (Exception e)
        {
            Logger logger = new Logger();
            logger.Log(e.ToString());

            const string title = "Glimpse";
            
            string logLocation = Path.Combine(IConfigManager.BaseDir, "LastSession.log");
            string message = $"Oops! Glimpse crashed.\nLog file at: {logLocation}\n\nPlease send log file + the following error to the developers:\n{e}";

            SDL.ShowSimpleMessageBox(SDL.MessageBoxFlags.Error, title, message, new SDL.Window());
        }
#endif
    }

    [Flags]
    public enum CommunicationFlags : byte
    {
        None = 0,
        
        PlayFile = 1 << 0
    }
}