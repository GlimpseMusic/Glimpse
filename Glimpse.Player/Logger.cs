using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Glimpse.Player;

public static class Logger
{
    [Conditional("DEBUG")]
    public static void Log(string message, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string file = "")
    {
        string localFile = Path.GetFileName(file);
        
        Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [LOG] ({localFile}:{lineNumber}) {message}");
    }
}