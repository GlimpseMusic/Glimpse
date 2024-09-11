using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Glimpse.Graphics.GLUtils;

public static class Resource
{
    public static byte[] Load(Assembly assembly, string name)
    {
        using Stream stream = assembly.GetManifestResourceStream(name);

        if (stream == null)
            throw new Exception($"Failed to load resource \"{name}\" in assembly {assembly}");

        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);

        return memStream.ToArray();
    }

    public static string LoadString(Assembly assembly, string name, Encoding encoding)
    {
        return encoding.GetString(Load(assembly, name));
    }

    public static string LoadString(Assembly assembly, string name)
        => LoadString(assembly, name, Encoding.UTF8);
}