using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Glimpse.Api;
using Glimpse.Player;
using Glimpse.Player.Configs;

public static class GlimpseCli
{
    public static void Main(string[] args)
    {
        Logger.Log("Program Start");
        
        if (args.Length == 0)
        {
            PrintHelp();
            return;
        }
        
        Logger.Log($"args.Length = {args.Length}");
        
        List<string> files = new List<string>();
        float? volume = null;
        double? speed = null;
        int currentFile = 0;

        int argIndex = 0;
        while (ReadArg(args, ref argIndex, out string arg))
        {
            if (arg.StartsWith('-'))
            {
                switch (arg)
                {
                    case "--help" or "-h":
                        PrintHelp();
                        return;
                    
                    case "--volume" or "-v":
                    {
                        if (ReadArg(args, ref argIndex, out arg) && float.TryParse(arg, out float vol))
                        {
                            volume = vol;
                            continue;
                        }
                        
                        PrintHelp();
                        Console.WriteLine();
                        Console.WriteLine("ERROR: Volume was not parsable.");
                        return;
                    }
                
                    case "--speed" or "-s":
                    {
                        if (ReadArg(args, ref argIndex, out arg) && double.TryParse(arg, out double spd))
                        {
                            speed = spd;
                            continue;
                        }
                        
                        PrintHelp();
                        Console.WriteLine();
                        Console.WriteLine("ERROR: Speed was not parsable.");
                        return;
                    }

                    case "--track" or "-t":
                    {
                        if (ReadArg(args, ref argIndex, out arg) && int.TryParse(arg, out int trackNumber))
                        {
                            currentFile = trackNumber - 1;
                            continue;
                        }

                        PrintHelp();
                        Console.WriteLine();
                        Console.WriteLine("ERROR: Track number was not parsable.");
                        return;
                    }
                    
                    default:
                        PrintHelp();
                        Console.WriteLine();
                        Console.WriteLine($"ERROR: Invalid argument \"{arg}\".");
                        return;
                }
            }
            else
            {
                string fileName = arg.Trim('"');

                if (File.Exists(fileName))
                {
                    files.Add(fileName);
                }
                else if (Directory.Exists(fileName))
                {
                    foreach (string file in Directory.EnumerateFiles(fileName, "*.*", SearchOption.AllDirectories).Where(s => Path.GetExtension(s) is ".mp3" or ".ogg" or ".wav" or ".flac"))
                        files.Add(file);
                }
                else
                {
                    PrintHelp();
                    Console.WriteLine();
                    Console.WriteLine($"ERROR: Argument {argIndex}: An invalid file was provided.");
                    return;
                }
            }
        }
        
        Logger.Log($"files.Count = {files.Count}");

        if (files.Count == 0)
        {
            PrintHelp();
            Console.WriteLine();
            Console.WriteLine("ERROR: No file was provided.");
            return;
        }

        Logger.Log("Create Audio Player");
        AudioPlayer player = new AudioPlayer();
        player.Config.Volume = volume ?? player.Config.Volume;
        player.Config.SpeedAdjust = speed ?? player.Config.SpeedAdjust;
        
        foreach (string path in files)
            player.QueueTrack(path, QueueSlot.AtEnd);

        player.ChangeTrack(0);
        
        PrintConsoleText(player.TrackInfo, 0, player.TrackLength, player.TrackState, currentFile, files.Count);

        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            ResetConsole();
        };
        
        Console.CursorVisible = false;

        while (true)
        {
            int elapsed = player.ElapsedSeconds;
            int total = player.TrackLength;

            (int left, int top) = Console.GetCursorPosition();
            Console.SetCursorPosition(left, top - 8);
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
                        goto EXIT;

                    case ConsoleKey.OemPeriod:
                    {
                        // TODO: This needs to be in a method.
                        currentFile++;
                        if (currentFile >= files.Count)
                        {
                            player.Stop();
                            goto EXIT;
                        }

                        player.Next();

                        break;
                    }

                    case ConsoleKey.OemComma:
                    {
                        currentFile--;
                        if (currentFile < 0)
                            currentFile = 0;
                
                        player.Previous();

                        break;
                    }
                }
            }
            
            Thread.Sleep(125);
        }
        
        EXIT: ;
        
        Logger.Log("Quitting.");
        
        ResetConsole();
        
        Logger.Log("Disposing audio player");
        player.Dispose();
    }

    private static void PrintConsoleText(TrackInfo info, int elapsed, int total, TrackState state, int track, int totalTracks)
    {
        int padAmount = Console.BufferWidth;
        
        Console.WriteLine($"Track:  {track + 1} / {totalTracks}".PadRight(padAmount));
        Console.WriteLine($"Title:  {info.Title}".PadRight(padAmount));
        Console.WriteLine($"Artist: {info.Artist}".PadRight(padAmount));
        Console.WriteLine($"Album:  {info.Album}".PadRight(padAmount));
        
        Console.WriteLine();
        
        Console.WriteLine(state.ToString().PadRight(60));
        Console.Write($"{elapsed / 60}:{elapsed % 60:00} [");

        int progress = (int) (((double) elapsed / total) * 51) - 1;
    
        for (int i = 0; i < 50; i++)
        {
            if (i <= progress)
                Console.Write('=');
            else
                Console.Write('-');
        }
    
        Console.WriteLine($"] {total / 60}:{total % 60:00}".PadRight(padAmount));
    }

    private static void ResetConsole()
    {
        Console.CursorVisible = true;
        Console.ResetColor();
    }

    private static bool ReadArg(string[] args, ref int index, out string arg)
    {
        arg = null;
        
        if (index >= args.Length)
            return false;

        arg = args[index++];
        return true;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
                          glimpsecli

                          Usage: glimpsecli [options] <files/directories>

                          Options:
                              --track <n>, -t <n>
                                  Start at track n.
                              --volume <v>, -v <v>
                                  Change the playback volume, where a value of 1.0 is 100% volume.
                              --speed <s>, -s <s>
                                  Change the playback speed, where a value of 1.0 is 100% speed;
                          """);
    }
}

