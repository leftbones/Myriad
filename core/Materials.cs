using Tommy;
using Calcium;
using Raylib_cs;

namespace Myriad;

struct Material() {
    public string Type;         // Solid, Liquid, Gas, Powder
    public string ID;           // Unique identifier
    public string Name;         // UI display name
    public string Color;        // Base color
    public int Offset;          // Amount of color variation
    public int Lifespan;        // Number of ticks before decay
    public int Health;          // Amount of damage before death
    public int Viscosity;       // Flow rate of liquids
    public int Softness;        // Spread rate of powders

    public List<string> Tags = [];
    public List<Reaction> Reactions = [];
}

struct Reaction() {
    public int Chance;
    public (string, string) Input;
    public (string, string) Output;
}

static class Materials {
    public static Dictionary<string, Material> Index = [];
    public static List<string> ByID = [];

    private static DateTime _latestLoadTime = DateTime.Now;

    public static void Init() {
        Pepper.Log("Loading materials...", LogType.System);

        var MaterialData = Directory.EnumerateFiles("materials", "*.toml", SearchOption.AllDirectories);
        foreach (var Data in MaterialData) {
            var Table = TOML.Parse(File.OpenText(Data));

            // Material Data
            var M = new Material() {Type = Table["Material"]["type"],
                                    ID = Table["Material"]["name"],
                                    Name = Table["Material"]["ui_name"],
                                    Color = Table["Material"]["color"] == "Tommy.TomlLazy" ? "FF00FFFF" : Table["Material"]["color"],
                                    Offset = Table["Material"]["offset"] == "Tommy.TomlLazy" ? 0 : Table["Material"]["offset"],
                                    Lifespan = Table["Material"]["lifespan"] == "Tommy.TomlLazy" ? 0 : Table["Material"]["lifespan"],
                                    Health = Table["Material"]["health"] == "Tommy.TomlLazy" ? 0 : Table["Material"]["health"],
                                    Viscosity = Table["Material"]["viscosity"] == "Tommy.TomlLazy" ? 0 : Table["Material"]["viscosity"],
                                    Softness = Table["Material"]["softness"] == "Tommy.TomlLazy" ? 0 : Table["Material"]["softness"]};

            // Tags + Reactions
            var Tags = new List<string>();
            foreach (var Tag in Table["Material"]["tags"]) {
                Tags.Add(Tag.ToString());
            }

            var Reactions = new List<Reaction>();
            if (Table.HasKey("Reaction")) {
                foreach (TomlNode Node in Table["Reaction"]) {
                    var R = new Reaction() {Chance = Node["chance"],
                                            Input = (Node["input_1"], Node["input_2"]),
                                            Output = (Node["output_1"], Node["output_2"])};
                    Reactions.Add(R);
                }
            }

            M.Tags = Tags;
            M.Reactions = Reactions;

            // Logging
            if (Config.VerboseLogging) {
                Pepper.Log($"Material: {M.Name} ({M.ID})");
                if (M.Reactions.Count > 0) {
                    foreach (var R in M.Reactions) {
                        Pepper.Log($"   - Reaction: {R.Input.Item1} + {R.Input.Item2} -> {R.Output.Item1} + {R.Output.Item2} ({R.Chance}%)");
                    }
                }
            }

            Index.Add(M.ID, M);
        }

        // Add all materials to the ByID list
        foreach (var Material in Index) {
            ByID.Add(Material.Value.ID);
        }

        Pepper.Log("Materials initialized", LogType.System);
    }

    // Rescan the materials directory and update any changes
    public static void Reload() {
        var MaterialData = Directory.EnumerateFiles("materials", "*.toml", SearchOption.AllDirectories).Where(x => File.GetLastWriteTime(x) > _latestLoadTime);
        foreach (var Data in MaterialData) {
            var Table = TOML.Parse(File.OpenText(Data));
            Index.Remove(Table["Material"]["name"]);

            // Material Data
            var M = new Material() {Type = Table["Material"]["type"],
                                    ID = Table["Material"]["name"],
                                    Name = Table["Material"]["ui_name"],
                                    Color = Table["Material"]["color"] == "Tommy.TomlLazy" ? "FF00FFFF" : Table["Material"]["color"],
                                    Offset = Table["Material"]["offset"] == "Tommy.TomlLazy" ? 0 : Table["Material"]["offset"],
                                    Lifespan = Table["Material"]["lifespan"] == "Tommy.TomlLazy" ? 0 : Table["Material"]["lifespan"],
                                    Health = Table["Material"]["health"] == "Tommy.TomlLazy" ? 0 : Table["Material"]["health"],
                                    Viscosity = Table["Material"]["viscosity"] == "Tommy.TomlLazy" ? 0 : Table["Material"]["viscosity"],
                                    Softness = Table["Material"]["softness"] == "Tommy.TomlLazy" ? 0 : Table["Material"]["softness"]};

            // Tags + Reactions
            var Tags = new List<string>();
            foreach (var Tag in Table["Material"]["tags"]) {
                Tags.Add(Tag.ToString());
            }

            var Reactions = new List<Reaction>();
            if (Table.HasKey("Reaction")) {
                foreach (TomlNode Node in Table["Reaction"]) {
                    var R = new Reaction() {Chance = Node["chance"],
                                            Input = (Node["input_1"], Node["input_2"]),
                                            Output = (Node["output_1"], Node["output_2"])};
                    Reactions.Add(R);
                }
            }

            M.Tags = Tags;
            M.Reactions = Reactions;

            Index.Add(M.ID, M);
        }

        _latestLoadTime = DateTime.Now;
        Pepper.Log("Materials reloaded", LogType.System);
    }

    // Quick access to material data
    public static string GetName(string id) => Index[id].Name;
    public static string GetType(string id) => Index[id].Type;
    public static List<string> GetTags(string id) => Index[id].Tags;
    public static List<Reaction> GetReactions(string id) => Index[id].Reactions;

    // Create a new instance of a material
    public static Pixel New(string id, Color? color=null, int? lifespan=null, int? health=null) {
        if (id == "air") {
            return new Pixel();
        }

        var M = Index[id];
        var C = color ?? Canvas.ShiftColor(Canvas.HexColor(Index[id].Color), RNG.Range(-M.Offset, M.Offset));
        var L = lifespan ?? Index[id].Lifespan;
        var H = health ?? Index[id].Health;
        var P = new Pixel(id, C, L);
        return P;
    }

    // Powder Behavior
    public static void TickPowder(World W, Pixel P, Vector2i Pos) {
        if (W.ValidSwap(Pos, Pos + Direction.Down)) { return; }
        var Dir = Direction.Random(Direction.DiagonalDown);

        if (RNG.Chance(Index[P.ID].Softness)) {
            if (RNG.CoinFlip() && W.ValidSwap(Pos, Pos + Direction.Left)) { return; }
            if (W.ValidSwap(Pos, Pos + Direction.Right)) { return; }
        }

        if (W.ValidSwap(Pos, Pos + Dir)) { return; }
        if (W.ValidSwap(Pos, Pos + Direction.FlipH(Dir))) { return; }
    }

    // Liquid Behavior
    public static void TickLiquid(World W, Pixel P, Vector2i Pos) {
        if (W.ValidSwap(Pos, Pos + Direction.Down)) { return; }
        if (RNG.Chance(Index[P.ID].Viscosity)) { return; }
        var Dir = Direction.Random(Direction.Horizontal);
        if (W.ValidSwap(Pos, Pos + Dir)) { return; }
        if (W.ValidSwap(Pos, Pos + Direction.Reverse(Dir))) { return; }
    }

    // Gas Behavior
    public static void TickGas(World W, Pixel P, Vector2i Pos) {
        var Moves = Direction.Shuffled(Direction.UpperHalf);
        foreach (var Move in Moves) {
            if (RNG.Chance(75) && W.ValidSwap(Pos, Pos + Move)) { return; }
        }
    }
}