using System;
using System.Threading;
using Glimpse.Player;
using TagLib;

public static class GlimpseCli
{
    public static void Main(string[] args)
    {
        string file = null;
        float volume = 1.0f;
        double speed = 1.0;

        int argIndex = 0;
        while (ReadArg(args, ref argIndex, out string arg))
        {
            switch (arg)
            {
                case "--volume" or "-v":
                {
                    if (ReadArg(args, ref argIndex, out arg) && float.TryParse(arg, out volume)) continue;
                    Console.WriteLine("Error while parsing volume.");
                    return;
                }
                
                case "--speed" or "-s":
                {
                    if (ReadArg(args, ref argIndex, out arg) && double.TryParse(arg, out speed)) continue;
                    Console.WriteLine("Error while parsing speed.");
                    return;
                }

                default:
                {
                    file ??= arg;
                    continue;
                }
            }
        }

        AudioPlayer player = new AudioPlayer(new PlayerSettings(48000) { Volume = volume, SpeedAdjust = speed});
        player.ChangeTrack(file);
        player.Play();
        
        PrintConsoleText(player.TrackInfo, 0, player.TrackLength, player.TrackState);

        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            ResetConsole();
        };
        
        Console.CursorVisible = false;

        while (player.TrackState != TrackState.Stopped)
        {
            int elapsed = player.ElapsedSeconds;
            int total = player.TrackLength;

            (int left, int top) = Console.GetCursorPosition();
            Console.SetCursorPosition(left, top - 6);
            PrintConsoleText(player.TrackInfo, elapsed, total, player.TrackState);

            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.P)
                {
                    if (player.TrackState == TrackState.Playing)
                        player.Pause();
                    else
                        player.Play();
                }
            }
            
            Thread.Sleep(125);
        }
        
        ResetConsole();
    }

    private static void PrintConsoleText(TrackInfo info, int elapsed, int total, TrackState state)
    {
        Console.WriteLine($"Title:  {info.Title}");
        Console.WriteLine($"Artist: {info.Artist}");
        Console.WriteLine($"Album:  {info.Album}");
        
        Console.WriteLine();
        
        Console.WriteLine(state);
        Console.Write($"{elapsed / 60}:{elapsed % 60:00} [");

        int progress = (int) (((double) elapsed / total) * 51) - 1;
    
        for (int i = 0; i < 50; i++)
        {
            if (i <= progress)
                Console.Write('=');
            else
                Console.Write('-');
        }
    
        Console.WriteLine($"] {total / 60}:{total % 60:00}");
    }

    private static void ResetConsole()
    {
        Console.CursorVisible = true;
    }

    private static bool ReadArg(string[] args, ref int index, out string arg)
    {
        arg = null;
        
        if (index >= args.Length)
            return false;

        arg = args[index++];
        return true;
    }
}

