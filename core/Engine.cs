using System.Diagnostics;
using Calcium;
using ImGuiNET;
using Raylib_cs;
using static Raylib_cs.Raylib;
namespace Myriad;

// TODO:
// - Add a config option to disable debug statistics tracking

static class Engine {
    public static World World { get; private set; }
    public static int TicksPerSecond = 150;
    public static int MaxTicksPerFrame = 2;
    public static double TickInterval = 1000.0 / TicksPerSecond;
    public static int MaxConcurrency = Environment.ProcessorCount;

    public static Dictionary<Vector2i, Pixel> PixelBuffer = [];

    public static bool Paused { get; private set; }
    public static bool ShouldQuit { get; private set; }

    public static Vector2i MousePos => new ((int)Math.Round((double)GetMouseX() / Global.PixelScale),
                                            (int)Math.Round((double)GetMouseY() / Global.PixelScale));

    public static List<Timer> Timers = [];

    private static int _storedSpeed;
    private static bool _pauseNextTick;
    private static bool _halted;

    private static float _tpsUpdateTimer;
    private static int _tpsTickCounter;
    private static int _actualTPS;


    // Initialize all engine components
    public static void Init() {
        Config.Init();
        Materials.Init();
        Canvas.Init();

        World = new World(Config.Resolution.X / Global.PixelScale, Config.Resolution.Y / Global.PixelScale);

        if (Config.Multithreading) {
            Pepper.Warn($"Max concurrency set to {MaxConcurrency}", LogType.Engine);
        }
        Pepper.Log("Engine initialized", LogType.System);

        if (Config.DebugMode) {
            Debbie.Init();
        }
    }

    // Set the TPS of the Engine, minimum allowed is 1
    public static void SetTicksPerSecond(int tps) {
        TicksPerSecond = Math.Max(tps, 1);
        TickInterval = 1000.0 / TicksPerSecond;
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
        if (Config.DebugMode && IsKeyPressed(KeyboardKey.Tab)) { Debbie.ShowCommandPrompt = !Debbie.ShowCommandPrompt; }

        if (!Config.DebugMode || !ImGui.GetIO().WantCaptureKeyboard) {
            if (IsKeyPressed(KeyboardKey.Enter)) { Debbie.ApplyCommand("r"); }
            if (IsKeyPressed(KeyboardKey.Slash)) { Debbie.ShowHelpWindow = !Debbie.ShowHelpWindow; }
            if (IsKeyPressed(KeyboardKey.D)) { Config.DebugMode = !Config.DebugMode; }
            if (IsKeyPressed(KeyboardKey.F)) { Config.ShowFPS = !Config.ShowFPS; }
            if (IsKeyPressed(KeyboardKey.X)) { Cheats.FillWorld(World, "air"); }

            if (Config.DebugMode) {
                if (IsKeyPressed(KeyboardKey.C)) { Config.DrawChunkBorders = !Config.DrawChunkBorders; }
                if (IsKeyPressed(KeyboardKey.B)) { Config.DrawUpdateRects = !Config.DrawUpdateRects; }
                if (IsKeyPressed(KeyboardKey.R)) { Materials.ReloadMaterials(); }
            }

            if (IsKeyPressed(KeyboardKey.Space)) {
                if (Paused) {
                    SetTicksPerSecond(_storedSpeed);
                    Paused = false;
                } else {
                    _storedSpeed = TicksPerSecond;
                    SetTicksPerSecond(0);
                    Paused = true;
                }
            }

            if (IsKeyPressed(KeyboardKey.T)) {
                if (Paused) {
                    SetTicksPerSecond(_storedSpeed);
                    _pauseNextTick = true;
                    Paused = false;
                }
            }

            if (IsKeyPressed(KeyboardKey.Escape)) {
                if (Debbie.ShowCommandPrompt) { Debbie.ShowCommandPrompt = false; }
                else { Quit(); }
            }
        }

        // Mouse
        if (!Config.DebugMode || !ImGui.GetIO().WantCaptureMouse) {
            // Paint
            if (IsMouseButtonDown(MouseButton.Left) && World.InBounds(MousePos)) {
                var BrushType = Materials.Index[Canvas.BrushMaterial].Type == "solid" ? 1 : 50;
                for (int x = -Canvas.BrushSize; x <= Canvas.BrushSize; x++) {
                    for (int y = -Canvas.BrushSize; y <= Canvas.BrushSize; y++) {
                        if (RNG.Odds(BrushType) && World.InBounds(MousePos + new Vector2i(x, y))) {
                            if (Canvas.PaintOnTop || World.IsEmpty(MousePos + new Vector2i(x, y))) {
                                var P = Materials.New(Canvas.BrushMaterial);
                                PixelBuffer[MousePos + new Vector2i(x, y)] = P;
                            }
                        }
                    }
                }
            }

            // Erase
            if (IsMouseButtonDown(MouseButton.Right) && World.InBounds(MousePos)) {
                for (int x = -Canvas.BrushSize; x <= Canvas.BrushSize; x++) {
                    for (int y = -Canvas.BrushSize; y <= Canvas.BrushSize; y++) {
                        if (World.InBounds(MousePos + new Vector2i(x, y))) {
                            var P = Materials.New("air");
                            World.Set(MousePos + new Vector2i(x, y), P);
                        }
                    }
                }
            }
        }
    }

    // Tick the World
    public static void Tick() {
        _tpsTickCounter++;
        World.Update();
    }

    // Update all Engine components
    public static void Update() {
        // Update the TPS counter -- FIXME: Seemingly accurate except when catching up from being behind, it exceeds the target TPS
        if (_tpsUpdateTimer >= 1.0) {
            _actualTPS = _tpsTickCounter;
            _tpsTickCounter = 0;
            _tpsUpdateTimer = 0;
        }

        Input();

        // Add all Pixels from the Engine's PixelBuffer and then clear it
        foreach (var P in PixelBuffer) { World.Set(P.Key, P.Value); }
        PixelBuffer.Clear();

        if (_halted) { return; }

        // Update Timers
        var DT = GetFrameTime();
        _tpsUpdateTimer += DT;

        for (int i = Timers.Count - 1; i >= 0; i--) {
            var T = Timers[i];
            T.Tick(DT);

            if (T.Done && !T.Repeat) {
                Timers.RemoveAt(i);
            }
        }

        if (_pauseNextTick) {
            SetTicksPerSecond(0);
            _pauseNextTick = false;
            Paused = true;
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

        if (Config.ShowTPS) {
            var TPSText = $"TPS: {_actualTPS}";
            var Offset = MeasureTextEx(Canvas.DefaultFont, TPSText, Canvas.DefaultFontSize, Canvas.DefaultFontSpacing);
            Canvas.DrawText(TPSText, Config.Resolution.X - (int)Offset.X - 10, 32);
        }
    }

    // Exit safely
    public static void Quit() {
        ShouldQuit = true;
    }
}
