using System.Numerics;

namespace Myriad;

static class Global {
    public static int PixelScale = 4;
    public static int ChunkSize = 50;

    public static Vector2 Gravity = new Vector2(0, 0.1f);
    public static Vector2 ParticleGravity = new Vector2(0, 0.05f);
}