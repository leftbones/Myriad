using System.Numerics;
using Raylib_cs;

namespace Myriad.Core;

internal static class Global {
    public static int PixelScale = 4;
    public static int ChunkSize = 50;

    public static Vector2 Gravity = new Vector2(0, 0.1f);
    public static Vector2 ParticleGravity = new Vector2(0, 0.05f);

    public static Color BackgroundColor = new Color(25, 40, 45, 255);

    public static Color[] FireColors = [Color.Red, Color.Orange, Color.Yellow];
}
