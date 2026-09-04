#!/usr/bin/env dotnet

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

string? outName = null;
string? runtime = null;
bool noPack = false;
bool noPlugins = false;
bool aot = false;

int argPos = 0;
while (ReadArg(args, ref argPos, out string? arg))
{
    if (arg.StartsWith('-'))
    {
        switch (arg)
        {
            case "--runtime" or "-r":
            {
                if (!ReadArg(args, ref argPos, out string? newRuntime))
                {
                    PrintError("No runtime identifier provided!");
                    return;
                }

                runtime = newRuntime;

                break;
            }
            
            case "--no-pack":
                noPack = true;
                break;
            case "--no-plugins":
                noPlugins = true;
                break;
            case "--aot":
                noPlugins = true;
                aot = true;
                break;
        }
    }
    else
    {
        outName = arg;
    }
}

if (outName == null)
{
    PrintError("No out name specified!");
    return;
}

if (runtime == null)
{
    if (OperatingSystem.IsWindows())
        runtime = "win";
    if (OperatingSystem.IsLinux())
        runtime = "linux";
    if (OperatingSystem.IsMacOS())
        runtime = "osx";

    switch (RuntimeInformation.OSArchitecture)
    {
        case Architecture.X64:
            runtime += "-x64";
            break;
        case Architecture.Arm64:
            runtime += "-arm64";
            break;
        default:
            PrintError($"Unsupported OS architecture \"{RuntimeInformation.OSArchitecture}\"");
            return;
    }
}

if (runtime is not ("win-x64" or "linux-x64" or "osx-arm64"))
{
    PrintError($"Runtime \"{runtime}\" is not supported!");
    return;
}

Console.WriteLine(runtime);

bool ReadArg(string[] args, ref int argPos, [NotNullWhen(true)] out string? arg)
{
    if (argPos >= args.Length)
    {
        arg = null;
        return false;
    }

    arg = args[argPos++];
    return true;
}

void PrintHelp()
{
    Console.WriteLine("""
                      USAGE: publish [OPTIONS] <OutName>
                      Publish and package (unless specified) Glimpse to the OutName.

                      Options:
                          --runtime <runtime>, -r <runtime>
                              Set the .NET runtime identifier to build for.
                              If not provided, the RID for the current OS will be used.

                          --no-pack
                              Do not package Glimpse into the platform's installer format.
                              
                          --no-plugins
                              Do not build or include plugins.
                              
                          --aot
                              Compile using NativeAOT. This will disable plugin support.
                      """);
}

void PrintError(string error)
{
    PrintHelp();
    Console.WriteLine();
    Console.WriteLine($"\e[31mERROR: {error}\e[0m");
}