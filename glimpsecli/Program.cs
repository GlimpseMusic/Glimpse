using System;
using System.Threading;
using Glimpse.Player;
using TagLib;

public static class GlimpseCli
{
    public static void Main(string[] args)
    {
        string file = args[0];

        AudioPlayer player = new AudioPlayer(new PlayerSettings(48000));
        player.ChangeTrack(file);
        player.Play();
        
        PrintConsoleText(player.TrackInfo, 0, player.TrackLength);

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
            Console.SetCursorPosition(left, top - 4);
            PrintConsoleText(player.TrackInfo, elapsed, total);

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

    private static void PrintConsoleText(TrackInfo info, int elapsed, int total)
    {
        Console.WriteLine($"Title:  {info.Title}");
        Console.WriteLine($"Artist: {info.Artist}");
        Console.WriteLine($"Album:  {info.Album}");
        
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

    public static void ResetConsole()
    {
        Console.CursorVisible = true;
    }
}

