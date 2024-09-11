using System.Drawing;
using System.Numerics;

namespace Glimpse.Graphics.GLUtils;

public static class Utils
{
    public static Vector4 Normalize(this Color color)
    {
        return new Vector4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
    }
}