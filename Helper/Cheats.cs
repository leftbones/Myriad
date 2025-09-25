using Calcium;
using Myriad.Core;

namespace Myriad.Helper;

internal static class Cheats {
    public static void FillWorld(World world, string material) {
        for (int x = 0; x < world.Size.X; x++) {
            for (int y = 0; y < world.Size.Y; y++) {
                Pixel P = Materials.New(material);
                world.Set(new Vector2i(x, y), P);
            }
        }
    }

    public static void NukeWorld(World world) {
        List<Vector2i> NukePoints = [];
        for (int x = 0; x < world.Size.X; x++) {
            for (int y = 0; y < world.Size.Y; y++) {
                if (RNG.Chance(1) && !world.IsEmpty(x, y)) {
                    NukePoints.Add(new Vector2i(x, y));
                }
            }
        }

        foreach (Vector2i Point in NukePoints) {
            world.MakeExplosion(Point, 10);
        }
    }
}
