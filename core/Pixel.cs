using Raylib_cs;

namespace Myriad;

class Pixel {
    public string ID { get; private set; }
    public Color Color { get; set; }
    public bool Sleeping { get; set; }
    public bool Updated { get; set; }
    public int Lifetime { get; set; }
    public int Lifespan { get; set; }
    public int Health { get; set; }

    public Pixel(string id="air", Color? color=null, int? lifespan=null, int? health=null) {
        ID = id;
        Color = color ?? Color.Magenta;
        Lifespan = lifespan ?? -1;
        Health = health ?? -1;
    }

    public void Tick() {
        if (Lifespan > 0) {
            Lifetime++;
        }
    }

    public void Damage(int amount) {
        Health = Math.Max(Health - amount, 0);
    }

    public void Stain(Color tint, float amount=0.5f) {
        Color = Canvas.BlendColor(Color, tint, amount);
    }
}