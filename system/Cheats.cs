using Calcium;

namespace Myriad;

static class Cheats {
    public static void FillWorld(World world, string material) {
        for (int x = 0; x < world.Size.X; x++) {
            for (int y = 0; y < world.Size.Y; y++) {
                var P = Materials.New(material);
                world.Set(new Vector2i(x, y), P);
            }
        }
    }

    public static void NukeWorld(World world) {
        for (int x = 0; x < world.Size.X; x++) {
            for (int y = 0; y < world.Size.Y; y++) {
                if (!world.IsEmpty(x, y) && RNG.Chance(1)) {
                    world.MakeExplosion(new Vector2i(x, y), 10);
                }
            }
        }
    }
}