using Calcium;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Myriad;

static class Engine {
    public static World World { get; private set; }
    public static double TicksPerSecond = 200;
    public static double TickRateMultiplier = 1.0;

    public static List<Timer> Timers = [];
    private static Timer _tickTimer;

    private static bool _halted = false;

    // Initialize all engine components
    public static void Init() {
        Config.Init();
        Canvas.Init();

        World = new World(Config.WindowSize.X / Global.PixelScale, Config.WindowSize.Y / Global.PixelScale);
        Pepper.Log("Engine initialized", LogType.System);

        // Simulation Timer
        _tickTimer = new Timer(1.0 / (TicksPerSecond * TickRateMultiplier), World.Update, true, true, true);
        Timers.Add(_tickTimer);
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
        var MousePos = new Vector2i(
            (int)Math.Round((double)GetMouseX() / Global.PixelScale),
            (int)Math.Round((double)GetMouseY() / Global.PixelScale)
        );

        if (IsMouseButtonDown(MouseButton.Left) && World.InBounds(MousePos)) {
            for (int x = -Canvas.BrushSize; x <= Canvas.BrushSize; x++) {
                for (int y = -Canvas.BrushSize; y <= Canvas.BrushSize; y++) {
                    if (RNG.Next(250) == 0 && World.InBounds(MousePos + new Vector2i(x, y))) {
                        World.Set(MousePos + new Vector2i(x, y), new Pixel(0));
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
        World.Draw();
        // Canvas.DrawText($"FPS: {GetFPS()}", 10, 10);
        // Canvas.DrawText($"Tick: {World.Tick}", 10, 30);
    }
}