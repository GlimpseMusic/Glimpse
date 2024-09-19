using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Glimpse.Api;
using Glimpse.Player.Configs;

namespace Glimpse.Player;

public class Logger : ILogger
{
    private static StreamWriter _writer;

    public Logger(string logDirectory)
    {
#if !DEBUG
        Directory.CreateDirectory(logDirectory);
        
        string fileLocation = Path.Combine(logDirectory, "LastSession.log");

        Console.WriteLine($"Initializing log file {fileLocation}");
        _writer = new StreamWriter(fileLocation)
        {
            AutoFlush = true
        };
#endif
    }
    
    //[Conditional("DEBUG")]
    public void Log(string message, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string file = "")
    {
        string localFile = Path.GetFileName(file);

        string logText = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [LOG] ({localFile}:{lineNumber}) {message}";
        Console.WriteLine(logText);
#if !DEBUG
        _writer.WriteLine(logText);
#endif
    }
}