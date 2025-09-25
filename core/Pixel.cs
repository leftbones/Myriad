using Myriad.Helper;
using Raylib_cs;

namespace Myriad.Core;

// TODO
// - Move the Damage and Stain methods to some sort of "PixelHandler" class or something, and move some parts of World that deal with Pixel properties to that class

public class Pixel {
    public string ID { get; private set; }
    public Color Color { get; set; }            = Color.Magenta;
    public bool Updated { get; set; }           = true;
    public int Lifetime { get; set; }           = 0;
    public int Lifespan { get; set; }           = -1;
    public int Health { get; set; }             = -1;

    // Status Conditions
    public bool Burning { get; set; }           = false;

    // Status Timers
    public int BurnTimer { get; set; }          = 0;
    public int TimeToBurn { get; set; }         = 1000;

    public Pixel(string id="air") {
        ID = id;
    }

    public void Damage(int amount) {
        Health = Math.Max(Health - amount, 0);
    }

    public void Stain(Color tint, float amount=0.5f) {
        if (Materials.GetType(ID) == "liquid") { return; }
        Color = Canvas.BlendColor(Color, tint, amount);
    }
}
