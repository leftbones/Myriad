using System.Text.RegularExpressions;
using Tommy;
using Calcium;
using Myriad.Helper;

namespace Myriad.Core;

// TODO:
// - Add some sort of handler for when we try to create a material but the data isn't found

internal class Material() {
    public string Type { get; set; }                // Solid, Liquid, Gas, Powder
    public string ID { get; set; }                  // Unique identifier
    public string Name { get; set; }                // UI display name
    public string Color { get; set; }               // Base color
    public int Offset { get; set; }                 // Amount of color variation
    public int Density { get; set; }                // "Weight" of the material
    public int Lifespan { get; set; }               // Number of ticks before decay
    public int Health { get; set; }                 // Amount of damage before death
    public int Viscosity { get; set; }              // Flow rate of liquids
    public int Softness { get; set; }               // Spread rate of powders

    public List<string> Tags { get; set; } = [];
    public List<Reaction> Reactions { get; set; } = [];
}

internal class Reaction() {
    public int Chance { get; set; }
    public string Reactant { get; set; }            // Material that causes the reaction
    public (string, string) Products { get; set; }  // Materials created by the reaction
}

internal static partial class Materials {
    public static Dictionary<string, Material> Index = [];
    public static List<string> ByID = [];

    public static Dictionary<string, Material> Solids = [];
    public static Dictionary<string, Material> Liquids = [];
    public static Dictionary<string, Material> Gases = [];
    public static Dictionary<string, Material> Powders = [];

    public static Dictionary<string, dynamic> Defaults = new() {
        { "Color", "ff00ffff" },
        { "Offset", 0 },
        { "Density_solid", 9999 },
        { "Density_liquid", 1000 },
        { "Density_gas", -1000 },
        { "Density_powder", 2000 },
        { "Lifespan", -1 },
        { "Health", -1 },
        { "Viscosity", 0 },
        { "Softness", 0 },
    };

    public static int Count => ByID.Count;
    public static List<string> Names => [.. Index.Values.Select(static m => m.Name)];

    private static DateTime _latestLoadTime = DateTime.Now;

    // Initialize the Materials
    public static void Init() {
        LoadMaterials(Directory.EnumerateFiles(Global.MaterialDataPath, "*.toml", SearchOption.AllDirectories));
        Pepper.Log("Materials initialized", LogType.System);
    }

    // Reload material data that has changed since the last load time
    public static void ReloadMaterials() {
        LoadMaterials(Directory.EnumerateFiles(Global.MaterialDataPath, "*.toml", SearchOption.AllDirectories).Where(static x => File.GetLastWriteTime(x) > _latestLoadTime));
        Pepper.Log("Materials reloaded", LogType.System);
    }

    // Scan the list of paths to material data files and load the data
    public static void LoadMaterials(IEnumerable<string> material_data_paths) {
        Pepper.Log("Reloading materials...", LogType.System);

        foreach (string Data in material_data_paths) {
            TomlTable Table = TOML.Parse(File.OpenText(Data));
            Index.Remove(Table["Material"]["name"]);
            Solids.Remove(Table["Material"]["name"]);
            Liquids.Remove(Table["Material"]["name"]);
            Gases.Remove(Table["Material"]["name"]);
            Powders.Remove(Table["Material"]["name"]);

            // Material Data
            Material M = new Material() {
                Type = Table["Material"]["type"],
                ID = Table["Material"]["name"],
                Name = Table["Material"]["ui_name"],
                Color = Table["Material"]["color"] == "Tommy.TomlLazy" ? Defaults["Color"] : Table["Material"]["color"],
                Offset = Table["Material"]["offset"] == "Tommy.TomlLazy" ? Defaults["Offset"] : Table["Material"]["offset"],
                Density = Table["Material"]["density"] == "Tommy.TomlLazy" ? Defaults["Density_" + Table["Material"]["type"]] : Table["Material"]["density"],
                Lifespan = Table["Material"]["lifespan"] == "Tommy.TomlLazy" ? Defaults["Lifespan"] : Table["Material"]["lifespan"],
                Health = Table["Material"]["health"] == "Tommy.TomlLazy" ? Defaults["Health"] : Table["Material"]["health"],
                Viscosity = Table["Material"]["viscosity"] == "Tommy.TomlLazy" ? Defaults["Viscosity"] : Table["Material"]["viscosity"],
                Softness = Table["Material"]["softness"] == "Tommy.TomlLazy" ? Defaults["Softness"] : Table["Material"]["softness"],
            };

            // Tags + Reactions
            List<string> Tags = [];
            foreach (TomlNode Tag in Table["Material"]["tags"]) {
                Tags.Add(Tag.ToString());
            }

            List<Reaction> Reactions = [];
            if (Table.HasKey("Reaction")) {
                foreach (TomlNode Node in Table["Reaction"]) {
                    Reaction R = new Reaction() {
                        Chance = Node["chance"],
                        Reactant = Node["reactant"],
                        Products = (Node["products"][0], Node["products"][1])
                    };
                    Reactions.Add(R);
                }
            }

            M.Tags = Tags;
            M.Reactions = Reactions;

            // Add to the Index
            Index.Add(M.ID, M);

            // Add to the appropriate type dictionary
            switch (M.Type) {
                case "Solid":
                    Solids.Add(M.ID, M);
                    break;
                case "Liquid":
                    Liquids.Add(M.ID, M);
                    break;
                case "Gas":
                    Gases.Add(M.ID, M);
                    break;
                case "Powder":
                    Powders.Add(M.ID, M);
                    break;
                default:
                    break;
            }

            // Add to the ByID list
            if (!ByID.Contains(M.ID)) {
                ByID.Add(M.ID);
            }
        }

        // Update the latest load time
        _latestLoadTime = DateTime.Now;
    }

    // Quick access to material data
    public static Material Get(string id) { return Index[id]; }
    public static string GetName(string id) { return Index[id].Name; }
    public static string GetType(string id) { return Index[id].Type; }
    public static int GetDensity(string id) { return Index[id].Density; }
    public static List<string> GetTags(string id) { return Index[id].Tags; }
    public static List<Reaction> GetReactions(string id) { return Index[id].Reactions; }
    public static List<Material> GetAllWithTag(string tag) { return [.. Index.Values.Where(M => M.Tags.Contains(tag))]; }
    public static bool HasTag(string id, string tag) { return Index[id].Tags.Contains(tag); }

    // Create a new instance of a material
    public static Pixel New(string id) {
        Material M = Index[id];
        Pixel P = new Pixel(id) {
            Color = Canvas.ShiftColor(Canvas.HexColor(M.Color), RNG.Range(-M.Offset, M.Offset)),
            Lifespan = M.Lifespan,
            Health = M.Health
        };

        P.Reactions = M.Reactions;
        return P;
    }

    // New Liquid Behavior
    public static void TickLiquidNew(World W, Pixel P, Vector2i Pos) {
        Vector2i Dir = RNG.CoinFlip() ? Direction.Left : Direction.Right;
        Vector2i OldPos = Pos;
        Vector2i NewPos = OldPos + Dir;

        for (int i = 0; i < 5; i++) {
        	if (W.ValidSwap(OldPos, OldPos + Direction.Down)) {
                Dir = Direction.Down;
                NewPos = OldPos + Dir;
                break;
         	}
            if (W.ValidSwap(OldPos, NewPos)) {
                OldPos = NewPos;
                NewPos += Dir;
            } else {
                Dir = Direction.FlipH(Dir);
                NewPos = OldPos + Dir;
            }
        }

        // W.CanReact(Get(P.ID), NewPos, Dir);

        if (!W.InBounds(NewPos + Dir)) { return; }
        Pixel N = W.Get(NewPos + Dir);

        Reaction R = P.Reactions.Find(r => r.Reactant == N.ID);
        if (R == null) { return; }

        if (RNG.Odds(R.Chance)) {
            W.Set(NewPos, New(R.Products.Item1));
            W.Set(NewPos + Dir, New(R.Products.Item2));
        }
    }

    // New Gas Behavior
    public static void TickGasNew(World W, Pixel P, Vector2i Pos) {
    	// No clue how to make this better
    }

    // New Powder Behavior
	public static void TickPowderNew(World W, Pixel P, Vector2i Pos) {
        if (W.ValidSwap(Pos, Pos + Direction.Down)) { return; }

        Vector2i Dir = Direction.Random(Direction.DiagonalDown);
        if (W.ValidSwap(Pos, Pos + Dir)) { return; }
        if (W.ValidSwap(Pos, Pos + Direction.FlipH(Dir))) { return; }
    }

    // Liquid Behavior
    public static void TickLiquid(World W, Pixel P, Vector2i Pos) {
        int V = Get(P.ID).Viscosity;
        Vector2i Dir = RNG.CoinFlip() ? Direction.Left : Direction.Right;
        int Dist = Global.ChunkSize / 5;
        Vector2i OldPos = Pos;
        Vector2i NewPos = OldPos + Dir;

        for (int i = 0; i < Dist; i++) {
            // Try to move down
            if (W.ValidSwap(OldPos, OldPos + Direction.Down)) {
                Dir = Direction.Down;
                NewPos = OldPos + Dir;
                break;
            }

            // Chance to stop moving
            if (RNG.Chance(1)) { return; }

            // Try to move sideways, change direction if blocked
            if (W.ValidSwap(OldPos, NewPos)) {
                OldPos = NewPos;
                NewPos += Dir;
            } else if (RNG.Chance(100 - V)) {
                Dir = Direction.FlipH(Dir);
                NewPos = OldPos + Dir;
            }
        }

        // Reaction
        if (W.CanReact(Get(P.ID), NewPos, Dir)) {
            return;
        }
    }

    // Gas Behavior
    public static void TickGas(World W, Pixel P, Vector2i Pos) {
        // Small chance to move horizontally
        if (RNG.Chance(2)) {
            foreach (Vector2i Dir in Direction.Shuffled(Direction.Horizontal)) {
                if (W.ValidSwap(Pos, Pos + Dir)) { return; }
            }
        }

        // Try to move based on density
        int Density = Index[P.ID].Density;

        // Zero density, try to move randomly
        if (Density == 0) {
            foreach (Vector2i Dir in Direction.Shuffled(Direction.Full)) {
                if (W.ValidSwap(Pos, Pos + Dir)) { return; } else if (RNG.Chance(25)) { return; }
            }
        }

        // Positive density, try to move downwards
        if (Density > 0) {
            foreach (Vector2i Dir in Direction.Shuffled(Direction.LowerHalf)) {
                if (W.ValidSwap(Pos, Pos + Dir)) { return; } else if (RNG.Chance(25)) { return; }
            }
        }

        // Negative density, try to move upwards
        if (Density < 0) {
            foreach (Vector2i Dir in Direction.Shuffled(Direction.UpperHalf)) {
                if (W.ValidSwap(Pos, Pos + Dir)) { return; } else if (RNG.Chance(25)) { return; }
            }
        }

        // Old Gas Behavior
        // if (RNG.CoinFlip()) {
        //     foreach (Vector2i Dir in Direction.Shuffled(Direction.Horizontal)) {
        //         if (W.ValidSwap(Pos, Pos + Dir)) { return; }
        //     }
        // }

        // Vector2i[] Moves = Direction.Shuffled(Direction.UpperHalf);
        // foreach (Vector2i Move in Moves) {
        //     if (RNG.Chance(75) && W.ValidSwap(Pos, Pos + Move)) { return; }
        // }
    }

    // Powder Behavior
    public static void TickPowder(World W, Pixel P, Vector2i Pos) {
        // Try to move down
        int S = Index[P.ID].Softness;
        if (RNG.Chance(100 - S) && W.ValidSwap(Pos, Pos + Direction.Down)) { return; }

        if (RNG.Chance(S)) {
            if (RNG.CoinFlip() && W.ValidSwap(Pos, Pos + Direction.Left)) { return; }
            if (W.ValidSwap(Pos, Pos + Direction.Right)) { return; }
        }

        // Try to move diagonal downwards
        Vector2i Dir = Direction.Random(Direction.DiagonalDown);
        if (W.ValidSwap(Pos, Pos + Dir)) { return; }
        if (W.ValidSwap(Pos, Pos + Direction.FlipH(Dir))) { return; }
    }

    // Tag checking regex
    public static bool IsTag(string str) { return IsTag().IsMatch(str); }
    public static bool ContainsTag(string str) { return ContainsTag().IsMatch(str); }
    public static string ExtractTag(string str) { return ContainsTag().Match(str).Value; }
    public static bool IsStatus(string str) { return IsStatus().IsMatch(str); }

    [GeneratedRegex(@"^\[.*\]$")]
    private static partial Regex IsTag();

    [GeneratedRegex(@"\[.*\]")]
    private static partial Regex ContainsTag();

    [GeneratedRegex(@"^\<.*\>$")]
    private static partial Regex IsStatus();
}
