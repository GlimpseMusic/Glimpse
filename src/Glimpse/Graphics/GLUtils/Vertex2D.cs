using System.Drawing;
using System.Numerics;

namespace Glimpse.Graphics.GLUtils;

public struct Vertex2D
{
    public Vector2 Position;

    public Vector2 TexCoord;

    public Vector4 Tint;

    public Vertex2D(Vector2 position, Vector2 texCoord, Vector4 tint)
    {
        Position = position;
        TexCoord = texCoord;
        Tint = tint;
    }
}