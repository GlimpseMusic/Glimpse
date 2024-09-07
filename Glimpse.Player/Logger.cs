using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Glimpse.Player.Configs;

namespace Glimpse.Player;

public static class Logger
{
    private static StreamWriter _writer;
    
    //[Conditional("DEBUG")]
    public static void Log(string message, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string file = "")
    {
#if !DEBUG
        if (_writer == null)
        {
            string fileLocation = Path.Combine(IConfig.BaseDir, "LastSession.log");

            Console.WriteLine($"Initializing log file {fileLocation}");
            _writer = new StreamWriter(fileLocation)
            {
                AutoFlush = true
            };
        }
#endif
        
        string localFile = Path.GetFileName(file);

        string logText = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [LOG] ({localFile}:{lineNumber}) {message}";
        Console.WriteLine(logText);
#if !DEBUG
        _writer.WriteLine(logText);
#endif
    }
}