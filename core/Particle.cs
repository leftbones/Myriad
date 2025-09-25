using System.Numerics;
using Raylib_cs;

namespace Myriad.Core;

public class Particle {
    public Pixel ContactPixel { get; set; }		// Pixel to turn into when coming in contact with another Pixel
    public Pixel ExpirePixel { get; set; }		// Pixel to turn into when expiring
    public Vector2 Position { get; set; }
    public Color Color { get; set; }
    public int Lifetime { get; set; }
    public int Lifespan { get; set; }
    public int Grace { get; set; }				// Number of ticks before the particle can interact with the world

    public Vector2 Velocity { get; set; }
    public Vector2 GravityDir { get; set; }

    public Particle(Vector2 position, Color color, Pixel on_contact=null, Pixel on_expire=null, int lifespan=-1, int grace=10) {
        ContactPixel = on_contact ?? Materials.New("air");
        ExpirePixel = on_expire ?? Materials.New("air");
        Position = position;
        Color = color;
        Lifespan = lifespan;
        Grace = grace;
        GravityDir = new Vector2(0, 1);
    }

    public void Tick() {
        if (Grace > 0) {
            Grace--;
        }

        if (Lifespan > 0) {
            Lifetime++;
        }
    }

    public void ApplyForce(Vector2 force) {
        Velocity += force;
    }
}
