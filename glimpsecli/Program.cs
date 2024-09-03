using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Glimpse.Player;
using Glimpse.Player.Configs;

public static class GlimpseCli
{
    public static void Main(string[] args)
    {
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
                    case "--volume" or "-v":
                    {
                        if (ReadArg(args, ref argIndex, out arg) && float.TryParse(arg, out float vol))
                        {
                            volume = vol;
                            continue;
                        }
                        
                        Console.WriteLine("Error while parsing volume.");
                        return;
                    }
                
                    case "--speed" or "-s":
                    {
                        if (ReadArg(args, ref argIndex, out arg) && double.TryParse(arg, out double spd))
                        {
                            speed = spd;
                            continue;
                        }
                        
                        Console.WriteLine("Error while parsing speed.");
                        return;
                    }

                    case "--track" or "-t":
                    {
                        if (ReadArg(args, ref argIndex, out arg) && int.TryParse(arg, out int trackNumber))
                        {
                            currentFile = trackNumber - 1;
                            continue;
                        }

                        Console.WriteLine("Error while parsing track number.");
                        return;
                    }
                    
                    default:
                        Console.WriteLine($"Invalid argument \"{arg}\".");
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
                    Console.WriteLine($"Argument {argIndex}: An invalid file was provided.");
                    return;
                }
            }
        }

        AudioPlayer.Initialize();
        AudioPlayer.Config.Volume = volume ?? AudioPlayer.Config.Volume;
        AudioPlayer.Config.SpeedAdjust = speed ?? AudioPlayer.Config.SpeedAdjust;
        
        AudioPlayer.ChangeTrack(files[currentFile]);
        AudioPlayer.Play();
        
        PrintConsoleText(AudioPlayer.TrackInfo, 0, AudioPlayer.TrackLength, AudioPlayer.TrackState, currentFile, files.Count);

        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            ResetConsole();
        };
        
        Console.CursorVisible = false;

        while (true)
        {
            int elapsed = AudioPlayer.ElapsedSeconds;
            int total = AudioPlayer.TrackLength;

            (int left, int top) = Console.GetCursorPosition();
            Console.SetCursorPosition(left, top - 7);
            PrintConsoleText(AudioPlayer.TrackInfo, elapsed, total, AudioPlayer.TrackState, currentFile, files.Count);

            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.P:
                    {
                        if (AudioPlayer.TrackState == TrackState.Playing)
                            AudioPlayer.Pause();
                        else
                            AudioPlayer.Play();

                        break;
                    }
                    
                    case ConsoleKey.Q:
                        AudioPlayer.Stop();
                        goto EXIT;

                    case ConsoleKey.RightArrow: // ] Key?? Maybe??
                    {
                        // TODO: This needs to be in a method.
                        currentFile++;
                        if (currentFile >= files.Count)
                        {
                            AudioPlayer.Stop();
                            goto EXIT;
                        }

                        AudioPlayer.ChangeTrack(files[currentFile]);
                        AudioPlayer.Play();

                        break;
                    }

                    case ConsoleKey.LeftArrow: // [ Key?? Maybe too??
                    {
                        currentFile--;
                        if (currentFile < 0)
                            currentFile = 0;
                
                        AudioPlayer.ChangeTrack(files[currentFile]);
                        AudioPlayer.Play();

                        break;
                    }
                }
            }

            if (AudioPlayer.TrackState == TrackState.Stopped)
            {
                currentFile++;
                if (currentFile >= files.Count)
                    break;
                
                AudioPlayer.ChangeTrack(files[currentFile]);
                AudioPlayer.Play();
            }
            
            Thread.Sleep(125);
        }
        
        EXIT: ;
        
        AudioPlayer.Dispose();
        
        ResetConsole();
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
    
        Console.WriteLine($"] {total / 60}:{total % 60:00}");
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
}

