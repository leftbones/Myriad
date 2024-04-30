using System.Numerics;
using Calcium;
using Raylib_cs;

namespace Myriad;

class Particle {
    public string PixelID { get; private set; }
    // public Pixel StoredPixel { get; private set; }   // TODO: This
    public Vector2 Position { get; set; }
    public Color Color { get; set; }
    public int Lifetime { get; set; }
    public int Lifespan { get; set; }
    public int Grace { get; set; }
    public bool Empty { get; set; }

    public Vector2 Velocity { get; set; }

    public Particle(string pixel_id, Vector2 position, Color color, int lifespan=-1, int grace=0, bool empty=false) {
        PixelID = pixel_id;
        Position = position;
        Color = color;
        Lifespan = lifespan;
        Grace = grace;
        Empty = empty;
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