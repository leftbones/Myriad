using System.Numerics;
using Calcium;
using Raylib_cs;

class Particle {
    public string PixelID { get; private set; }
    public Vector2 Position { get; set; }
    public Color Color { get; set; }
    public int Lifetime { get; set; }
    public int Lifespan { get; set; }
    public int Grace { get; set; }

    public Vector2 Velocity { get; set; }

    public Particle(string pixel_id, Vector2 position, Color color, int lifespan=0, int grace=0) {
        PixelID = pixel_id;
        Position = position;
        Color = color;
        Lifespan = lifespan;
        Grace = grace;
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