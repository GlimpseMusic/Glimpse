using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Glimpse.Player;

public static class GlimpseCli
{
    public static void Main(string[] args)
    {
        List<string> files = new List<string>();
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
                    files.Add(arg);
                    continue;
                }
            }
        }

        if (files.Count == 1 && Path.GetFileNameWithoutExtension(files[0]) == "*")
        {
            string fileName = files[0];
            files.Clear();
            
            foreach (string file in Directory.GetFiles(Path.GetDirectoryName(fileName), Path.GetFileName(fileName), SearchOption.AllDirectories))
            {
                files.Add(file);
            }
        }

        int currentFile = 0;

        using AudioPlayer player = new AudioPlayer(new PlayerSettings(48000) { Volume = volume, SpeedAdjust = speed});
        player.ChangeTrack(files[currentFile]);
        player.Play();
        
        PrintConsoleText(player.TrackInfo, 0, player.TrackLength, player.TrackState, currentFile, files.Count);

        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            ResetConsole();
        };
        
        Console.CursorVisible = false;

        bool alive = true;

        while (alive)
        {
            int elapsed = player.ElapsedSeconds;
            int total = player.TrackLength;

            (int left, int top) = Console.GetCursorPosition();
            Console.SetCursorPosition(left, top - 7);
            PrintConsoleText(player.TrackInfo, elapsed, total, player.TrackState, currentFile, files.Count);

            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.P:
                    {
                        if (player.TrackState == TrackState.Playing)
                            player.Pause();
                        else
                            player.Play();

                        break;
                    }
                    
                    case ConsoleKey.Q:
                        player.Stop();
                        alive = false;
                        break;

                    case ConsoleKey.Oem6: // ] Key?? Maybe??
                    {
                        // TODO: This needs to be in a method.
                        currentFile++;
                        if (currentFile >= files.Count)
                        {
                            player.Stop();
                            alive = false;
                            break;
                        }

                        player.ChangeTrack(files[currentFile]);
                        player.Play();

                        break;
                    }

                    case ConsoleKey.Oem4: // [ Key?? Maybe too??
                    {
                        currentFile--;
                        if (currentFile < 0)
                            currentFile = 0;
                
                        player.ChangeTrack(files[currentFile]);
                        player.Play();

                        break;
                    }
                }
            }

            if (player.TrackState == TrackState.Stopped)
            {
                currentFile++;
                if (currentFile >= files.Count)
                    break;
                
                player.ChangeTrack(files[currentFile]);
                player.Play();
            }
            
            Thread.Sleep(125);
        }
        
        ResetConsole();
    }

    private static void PrintConsoleText(TrackInfo info, int elapsed, int total, TrackState state, int track, int totalTracks)
    {
        Console.WriteLine($"Track:  {track + 1} / {totalTracks}");
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

