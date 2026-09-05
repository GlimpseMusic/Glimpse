#!/usr/bin/env -S dotnet --

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;

string outName = "Publish";
string? version = null;
string? runtime = null;
bool pack = true;
bool plugins = true;
bool noPDB = false;
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
                pack = false;
                break;
            case "--no-plugins":
                plugins = false;
                break;
            case "--no-pdb":
                noPDB = true;
                break;
            case "--aot":
                plugins = false;
                aot = true;
                break;

            default:
            {
                PrintError($"Unrecognized argument \"{arg}\".");
                return;
            }
        }
    }
    else
    {
        version = arg;
    }
}

if (version == null)
{
    PrintError("No version specified!");
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

string publishDir = Path.Combine(Environment.CurrentDirectory, outName);
if (Directory.Exists(publishDir))
    Directory.Delete(publishDir, true);

// ================ Publish Glimpse ================

string glimpseSrcDir = Path.Combine(Environment.CurrentDirectory, "src", "Glimpse");
//string glimpsecliSrcDir = Path.Combine(Environment.CurrentDirectory, "src", "glimpsecli");

List<string> glimpsePublishArgs =
[
    "publish",
    glimpseSrcDir,
    "-c", "Release",
    "-r", runtime,
    "-o", publishDir,
    $"-p:Version=\"{version}\"",
    "-p:GenerateDocumentationFile=false"
];

if (aot)
    glimpsePublishArgs.Add("-p:PublishAot=true");
if (noPDB)
    glimpsePublishArgs.Add("-p:DebugType=none");

if (!RunProcess("dotnet", glimpsePublishArgs))
{
    PrintError("Failed to build glimpse.", false);
    return;
}

// =================================================

// ==================== Plugins ====================
if (plugins)
{
    string pluginsBaseDir = Path.Combine(Environment.CurrentDirectory, "Plugins");
    string pluginsOutDir = Path.Combine(publishDir, "Plugins");
    string packageScriptLocation = Path.Combine(Environment.CurrentDirectory, "tools", "sdk", "package-plugin.cs");

    Directory.CreateDirectory(pluginsOutDir);

    // the publish script assumes that the built-in plugins are NOT in subdirectories
    foreach (string dir in Directory.GetDirectories(pluginsBaseDir))
    {
        bool hasPluginJson = false;
        foreach (string file in Directory.GetFiles(dir))
        {
            if (Path.GetFileName(file) == "Plugin.json")
            {
                hasPluginJson = true;
                break;
            }
        }

        if (!hasPluginJson)
            continue;

        string pluginName = Path.GetFileName(dir);

        if (!RunProcess("dotnet", packageScriptLocation, dir, "--no-pack", "--glimpse-version", version))
        {
            PrintError($"Failed to package plugin \"{pluginName}\".", false);
            return;
        }

        Directory.Move(Path.Combine(Environment.CurrentDirectory, pluginName), Path.Combine(pluginsOutDir, pluginName));
    }
}
// =================================================

// ==================== Cleanup ====================

string cwd = Environment.CurrentDirectory;
Environment.CurrentDirectory = publishDir;

File.Delete("Silk.NET.SDL.dll");

if (runtime.StartsWith("win"))
{
    File.Delete("libmixr.so");
    File.Delete("libmixr.dylib");
    File.Delete("libempress.so");
    File.Delete("SDL2.dll");
}
else if (runtime.StartsWith("linux"))
{
    File.Delete("mixr.dll");
    File.Delete("libmixr.dylib");
    File.Delete("libSDL2-2.0.so");
}
else if (runtime.StartsWith("osx"))
{
    File.Delete("mixr.dll");
    File.Delete("libmixr.so");
    File.Delete("libempress.so");
    File.Delete("libSDL2-2.0.dylib");
}

Environment.CurrentDirectory = cwd;

// =================================================

// ===================== Pack ======================
if (pack)
{
    if (runtime.StartsWith("win"))
    {
        string nsiDir = Path.Combine(Environment.CurrentDirectory, "packaging", "windows");
        string installerName = $"InstallGlimpse-{version}.exe";

        // zip it up before building the nsi so we don't bundle vc redist
        using MemoryStream zipStream = new MemoryStream();
        ZipFile.CreateFromDirectory(publishDir, zipStream);
        Console.WriteLine(zipStream.Length);

        // todo use httpclient
        using (WebClient client = new WebClient())
            client.DownloadFile("https://aka.ms/vc14/vc_redist.x64.exe", Path.Combine(publishDir, "vc_redist.x64.exe"));

        if (!RunProcess("makensis", $"-DVERSION={version}", $"-DPUBLISHDIR={publishDir}", Path.Combine(nsiDir, "glimpse.nsi")))
        {
            PrintError("Failed to package NSIS file.", false);
            return;
        }

        // hack to clear the contents of the publish directory
        Directory.Delete(publishDir, true);
        Directory.CreateDirectory(publishDir);

        using FileStream zipWriteStream = File.Create(Path.Combine(publishDir, $"{outName}.zip"));
        zipStream.WriteTo(zipWriteStream);

        File.Move(Path.Combine(nsiDir, installerName), Path.Combine(publishDir, installerName));
    }
    else if (runtime.StartsWith("linux"))
    {
        string packagingDir = Path.Combine(Environment.CurrentDirectory, "packaging", "linux");

        string tarballDir = Path.Combine(packagingDir, "tarball");
        string binDir = Path.Combine(tarballDir, "bin");
        
        if (Directory.Exists(binDir))
            Directory.Delete(binDir, true);

        Directory.CreateDirectory(binDir);
        foreach (string file in Directory.GetFiles(publishDir, "*", SearchOption.AllDirectories))
        {
            string dir = Path.GetRelativePath(publishDir, Path.GetDirectoryName(file));
            Directory.CreateDirectory(Path.Combine(binDir, dir));
            File.Copy(file, Path.Combine(binDir, dir, Path.GetFileName(file)));
        }
        
        using MemoryStream zipStream = new MemoryStream();
        ZipFile.CreateFromDirectory(tarballDir, zipStream);
        Console.WriteLine(zipStream.Length);
        
        string appImageDir = Path.Combine(packagingDir, "appimage");
        string usrDir = Path.Combine(appImageDir, "usr");
        string binaryDir = Path.Combine(usrDir, "bin");
        string appImageDest = $"Glimpse-{version}-{runtime}.AppImage";

        // reset state
        if (Directory.Exists(usrDir))
            Directory.Delete(usrDir, true);
        File.Delete(Path.Combine(appImageDir, ".DirIcon"));

        Directory.CreateDirectory(binaryDir);

        foreach (string file in Directory.GetFiles(publishDir, "*", SearchOption.AllDirectories))
        {
            string dir = Path.GetRelativePath(publishDir, Path.GetDirectoryName(file));
            Directory.CreateDirectory(Path.Combine(binaryDir, dir));
            File.Copy(file, Path.Combine(binaryDir, dir, Path.GetFileName(file)));
        }

        RunProcess("appimagetool-x86_64.AppImage", appImageDir, appImageDest);

        // hack to clear the contents of the publish directory
        Directory.Delete(publishDir, true);
        Directory.CreateDirectory(publishDir);

        File.Move(appImageDest, Path.Combine(publishDir, appImageDest));
        
        using FileStream zipWriteStream = File.Create(Path.Combine(publishDir, $"{outName}.zip"));
        zipStream.WriteTo(zipWriteStream);

        // cleanup garbage
        Directory.Delete(binDir, true);
        
        Directory.Delete(usrDir, true);
        File.Delete(Path.Combine(appImageDir, ".DirIcon"));
    }
}


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
                      USAGE: publish [OPTIONS] <version>
                      Publish and package (unless specified) Glimpse with the given version number.

                      Options:
                          --output <name>, -o <name>
                              Set the output name.
                              If not provided, the output name will be "Publish"
                      
                          --runtime <runtime>, -r <runtime>
                              Set the .NET runtime identifier to build for.
                              If not provided, the RID for the current OS will be used.

                          --no-pack
                              Do not package Glimpse into the platform's installer format.
                              
                          --no-plugins
                              Do not build or include plugins.
                              
                          --no-pdb
                              Do not export PDB files.
                              
                          --aot
                              Compile using NativeAOT. This will disable plugin support.
                      """);
}

void PrintError(string error, bool printHelp = true)
{
    if (printHelp)
    {
        PrintHelp();
        Console.WriteLine();
    }

    Console.WriteLine($"\e[31mERROR: {error}\e[0m");
}

bool RunProcess(string processName, params IEnumerable<string> args)
{
    Process process = new Process()
    {
        StartInfo = new ProcessStartInfo(processName, args)
    };

    if (!process.Start())
        return false;

    process.WaitForExit();
    return process.ExitCode == 0;
}