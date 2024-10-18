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
        var NukePoints = new List<Vector2i>();
        for (int x = 0; x < world.Size.X; x++) {
            for (int y = 0; y < world.Size.Y; y++) {
                if (RNG.Chance(1) && !world.IsEmpty(x, y)) {
                    NukePoints.Add(new Vector2i(x, y));
                }
            }
        }

        foreach (var Point in NukePoints) {
            world.MakeExplosion(Point, 10);
        }
    }
}