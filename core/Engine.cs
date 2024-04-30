using System.Numerics;
using Calcium;
using ImGuiNET;
using Raylib_cs;
using static Raylib_cs.Raylib;
namespace Myriad;

// TODO:
// - Add a config option to disable debug statistics tracking

static class Engine {
    public static World World { get; private set; }
    public static int TicksPerSecond = 200;
    public static double TickRateMultiplier = 1.0;

    public static bool ShouldQuit { get; private set; }

    public static Vector2i MousePos => new ((int)Math.Round((double)GetMouseX() / Global.PixelScale),
                                            (int)Math.Round((double)GetMouseY() / Global.PixelScale));

    public static List<Timer> Timers = [];
    private static Timer _tickTimer;

    private static bool _halted = false;

    // Initialize all engine components
    public static void Init() {
        Config.Init();
        Materials.Init();
        Canvas.Init();

        World = new World(Config.Resolution.X / Global.PixelScale, Config.Resolution.Y / Global.PixelScale);
        Pepper.Log("Engine initialized", LogType.System);

        if (Config.DebugMode) {
            Debbie.Init();
        }

        // Simulation Timer
        _tickTimer = new Timer(1.0 / (TicksPerSecond * TickRateMultiplier), World.Update, true, true, true);
        Timers.Add(_tickTimer);
    }

    public static void SetTicksPerSecond(int tps) {
        TicksPerSecond = tps;
        _tickTimer.Lifetime = 1.0 / (TicksPerSecond * TickRateMultiplier);
    }

    // Add a new Timer
    public static void AddTimer(double duration, Action action, bool start=true, bool repeat=false, bool fire_on_start=false) {
        Timers.Add(new Timer(duration, action, start, repeat, fire_on_start));
    }

    // Stop everything except the bare minimum
    public static void Halt() {
        _halted = true;
        Pepper.Log("Engine halted", LogType.System, LogLevel.Fatal);
    }

    // Handle input (temporary)
    public static void Input() {
        // Keyboard
        if (IsKeyPressed(KeyboardKey.Tab)) { Debbie.ShowCommandPrompt = true; }

        if (IsKeyPressed(KeyboardKey.Escape)) {
            if (Debbie.ShowCommandPrompt) { Debbie.ShowCommandPrompt = false; }
            else { Quit(); }
        }

        // Mouse
        if (ImGui.GetIO().WantCaptureMouse) { return; }
        if (IsMouseButtonDown(MouseButton.Left) && World.InBounds(MousePos)) {
            for (int x = -Canvas.BrushSize; x <= Canvas.BrushSize; x++) {
                for (int y = -Canvas.BrushSize; y <= Canvas.BrushSize; y++) {
                    if (World.InBounds(MousePos + new Vector2i(x, y))) {
                        if (World.IsEmpty(MousePos + new Vector2i(x, y))) {
                            var P = Materials.New(Canvas.BrushMaterial);
                            World.Set(MousePos + new Vector2i(x, y), P);
                        }
                    }
                }
            }
        }
    }

    // Update all Engine components
    public static void Update() {
        if (_halted) { return; }

        Input();

        // Update Timers
        for (int i = Timers.Count - 1; i >= 0; i--) {
            var T = Timers[i];
            T.Tick();

            if (T.Done && !T.Repeat) {
                Timers.RemoveAt(i);
            }
        }
    }

    // Draw all Engine components
    public static void Draw() {
        ClearBackground(Global.BackgroundColor);

        World.Draw();

        if (Config.DebugMode) {
            Debbie.Draw();
        }

        if (Config.ShowFPS) {
            var FPSText = $"FPS: {GetFPS()}";
            var Offset = MeasureTextEx(Canvas.DefaultFont, FPSText, Canvas.DefaultFontSize, Canvas.DefaultFontSpacing);
            Canvas.DrawText(FPSText, Config.Resolution.X - (int)Offset.X - 10, 10);
        }
    }

    // Exit safely
    public static void Quit() {
        ShouldQuit = true;
    }
}