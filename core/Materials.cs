using System.Text.RegularExpressions;
using Tommy;
using Calcium;

namespace Myriad;

// TODO:
// - Add some sort of handler for when we try to create a material but the data isn't found

class Material() {
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
    public List<Reaction> Reactions { get; set; }= [];
}

class Reaction() {
    public int Chance { get; set; }
    public (string, string) Input { get; set; }
    public (string, string) Output { get; set; }
}

static partial class Materials {
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
    public static List<string> Names => Index.Values.Select(m => m.Name).ToList();

    private static DateTime _latestLoadTime = DateTime.Now;

    // Initialize the Materials
    public static void Init() {
        LoadMaterials(Directory.EnumerateFiles("materials", "*.toml", SearchOption.AllDirectories));
        Pepper.Log("Materials initialized", LogType.System);
    }

    // Reload material data that has changed since the last load time
    public static void ReloadMaterials() {
        LoadMaterials(Directory.EnumerateFiles("materials", "*.toml", SearchOption.AllDirectories).Where(x => File.GetLastWriteTime(x) > _latestLoadTime));
        Pepper.Log("Materials reloaded", LogType.System);
    }

    // Scan the list of paths to material data files and load the data
    public static void LoadMaterials(IEnumerable<string> material_data_paths) {
        Pepper.Log("Reloading materials...", LogType.System);

        foreach (var Data in material_data_paths) {
            var Table = TOML.Parse(File.OpenText(Data));
            Index.Remove(Table["Material"]["name"]);
            Solids.Remove(Table["Material"]["name"]);
            Liquids.Remove(Table["Material"]["name"]);
            Gases.Remove(Table["Material"]["name"]);
            Powders.Remove(Table["Material"]["name"]);

            // Material Data
            var M = new Material() {
                Type = Table["Material"]["type"],
                ID = Table["Material"]["name"],
                Name = Table["Material"]["ui_name"],
                Color = Table["Material"]["color"] ==               "Tommy.TomlLazy"    ? Defaults["Color"]                                     : Table["Material"]["color"],
                Offset = Table["Material"]["offset"] ==             "Tommy.TomlLazy"    ? Defaults["Offset"]                                    : Table["Material"]["offset"],
                Density = Table["Material"]["density"] ==           "Tommy.TomlLazy"    ? Defaults["Density_" + Table["Material"]["type"]]      : Table["Material"]["density"],
                Lifespan = Table["Material"]["lifespan"] ==         "Tommy.TomlLazy"    ? Defaults["Lifespan"]                                  : Table["Material"]["lifespan"],
                Health = Table["Material"]["health"] ==             "Tommy.TomlLazy"    ? Defaults["Health"]                                    : Table["Material"]["health"],
                Viscosity = Table["Material"]["viscosity"] ==       "Tommy.TomlLazy"    ? Defaults["Viscosity"]                                 : Table["Material"]["viscosity"],
                Softness = Table["Material"]["softness"] ==         "Tommy.TomlLazy"    ? Defaults["Softness"]                                  : Table["Material"]["softness"],
            };

            // Tags + Reactions
            var Tags = new List<string>();
            foreach (var Tag in Table["Material"]["tags"]) {
                Tags.Add(Tag.ToString());
            }

            var Reactions = new List<Reaction>();
            if (Table.HasKey("Reaction")) {
                foreach (TomlNode Node in Table["Reaction"]) {
                    var R = new Reaction() {
                        Chance = Node["chance"],
                        Input = (Node["input"][0], Node["input"][1]),
                        Output = (Node["output"][0], Node["output"][1])
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
    public static Material Get(string id) => Index[id];
    public static string GetName(string id) => Index[id].Name;
    public static string GetType(string id) => Index[id].Type;
    public static int GetDensity(string id) => Index[id].Density;
    public static List<string> GetTags(string id) => Index[id].Tags;
    public static List<Reaction> GetReactions(string id) => Index[id].Reactions;
    public static List<Material> GetAllWithTag(string tag) => Index.Values.Where(M => M.Tags.Contains(tag)).ToList();
    public static bool HasTag(string id, string tag) => Index[id].Tags.Contains(tag);

    // Create a new instance of a material
    public static Pixel New(string id) {
        var M = Index[id];
        var P = new Pixel(id) {
            Color = Canvas.ShiftColor(Canvas.HexColor(M.Color), RNG.Range(-M.Offset, M.Offset)),
            Lifespan = M.Lifespan,
            Health = M.Health
        };
        return P;
    }

    // Liquid Behavior
    public static void TickLiquid(World W, Pixel P, Vector2i Pos) {
        var V = Get(P.ID).Viscosity;
        var Dir = RNG.CoinFlip() ? Direction.Left : Direction.Right;
        var Dist = Global.ChunkSize / 5;
        var OldPos = Pos;
        var NewPos = OldPos + Dir;

        for (int i = 0; i < Dist; i++) {
            // Try to move down
            if (W.ValidSwap(OldPos, OldPos + Direction.Down)) { return; }

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
    }

    // Gas Behavior
    public static void TickGas(World W, Pixel P, Vector2i Pos) {
        // Try to move upwwards
        if (RNG.CoinFlip()) {
            foreach (var Dir in Direction.Shuffled(Direction.Horizontal)) {
                if (W.ValidSwap(Pos, Pos + Dir)) { return; }
            }
        }

        var Moves = Direction.Shuffled(Direction.UpperHalf);
        foreach (var Move in Moves) {
            if (RNG.Chance(75) && W.ValidSwap(Pos, Pos + Move)) { return; }
        }
    }

    // Powder Behavior
    public static void TickPowder(World W, Pixel P, Vector2i Pos) {
        // Try to move down
        var S = Index[P.ID].Softness;
        if (RNG.Chance(100 - S) && W.ValidSwap(Pos, Pos + Direction.Down)) { return; }

        if (RNG.Chance(S)) {
            if (RNG.CoinFlip() && W.ValidSwap(Pos, Pos + Direction.Left)) { return; }
            if (W.ValidSwap(Pos, Pos + Direction.Right)) { return; }
        }

        // Try to move diagonal downwards
        var Dir = Direction.Random(Direction.DiagonalDown);
        if (W.ValidSwap(Pos, Pos + Dir)) { return; }
        if (W.ValidSwap(Pos, Pos + Direction.FlipH(Dir))) { return; }
    }

    // Tag checking regex
    public static bool IsTag(string str) => IsTag().IsMatch(str);                   // Checks if a string is a [tag] and not a [tag]_substr
    public static bool ContainsTag(string str) => ContainsTag().IsMatch(str);       // Checks if a string contains a [tag]
    public static string ExtractTag(string str) => ContainsTag().Match(str).Value;  // Extracts a [tag] from a [tag]_substr string

    [GeneratedRegex(@"^\[.*\]$")]
    private static partial Regex IsTag();

    [GeneratedRegex(@"\[.*\]")]
    private static partial Regex ContainsTag();
}
