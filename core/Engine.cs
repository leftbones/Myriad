using System.Numerics;
using Calcium;
using ImGuiNET;
using Myriad.Helper;
using Timer = Myriad.Helper.Timer;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Myriad.Core;

// TODO:
// - Add a config option to disable debug statistics tracking (or just pause it when debug is disabled)

internal static class Engine {
    public static World World { get; private set; }
    public static int TicksPerSecond = 150;
    public static int MaxTicksPerFrame = 5;
    public static long TickIntervalTicks { get; private set; } = TimeSpan.TicksPerSecond / TicksPerSecond;
    public static int MaxConcurrency = Environment.ProcessorCount;

    public static Dictionary<Vector2i, Pixel> PixelBuffer = [];

    public static bool Paused { get; private set; }
    public static bool TimingReset { get; set; }
    public static bool ShouldQuit { get; private set; }

    public static Vector2i MousePos => new((int)Math.Round((double)GetMouseX() / Global.PixelScale),
                                            (int)Math.Round((double)GetMouseY() / Global.PixelScale));

    public static List<Timer> Timers = [];

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
        TickIntervalTicks = TimeSpan.TicksPerSecond / TicksPerSecond;
    }

    // Pause the simulation
    public static void Pause() {
        if (!Paused) {
            Paused = true;
            TimingReset = true;
        }
    }

    // Resume the simulation
    public static void Resume() {
        if (Paused) {
            Paused = false;
            TimingReset = true;
        }
    }

    // Toggle pause state
    public static void TogglePause() {
        if (Paused) {
            Resume();
        }
        else {
            Pause();
        }
    }

    // Add a new Timer
    public static void AddTimer(double duration, Action action, bool start = true, bool repeat = false, bool fire_on_start = false) {
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

            // Pause and unpause the simulation
            if (IsKeyPressed(KeyboardKey.Space)) {
                TogglePause();
            }

            // Step forward one tick while paused
            if (IsKeyPressed(KeyboardKey.T)) {
                if (Paused) {
                    _pauseNextTick = true;
                    Resume();
                }
            }

            // Force quit the simulation
            if (IsKeyPressed(KeyboardKey.Escape)) {
                if (Debbie.ShowCommandPrompt) { Debbie.ShowCommandPrompt = false; }
                else { Quit(); }
            }
        }

        // Mouse
        if (!Config.DebugMode || !ImGui.GetIO().WantCaptureMouse) {
            // Paint
            if (IsMouseButtonDown(MouseButton.Left) && World.InBounds(MousePos)) {
                int BrushType = Materials.Index[Canvas.BrushMaterial].Type == "solid" ? 1 : 50;
                for (int x = -Canvas.BrushSize; x <= Canvas.BrushSize; x++) {
                    for (int y = -Canvas.BrushSize; y <= Canvas.BrushSize; y++) {
                        if (RNG.Odds(BrushType) && World.InBounds(MousePos + new Vector2i(x, y))) {
                            if (Canvas.PaintOnTop || World.IsEmpty(MousePos + new Vector2i(x, y))) {
                                Pixel P = Materials.New(Canvas.BrushMaterial);
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
                            Pixel P = Materials.New("air");
                            PixelBuffer[MousePos + new Vector2i(x, y)] = P;
                        }
                    }
                }
            }
        }
    }

    // Tick the World
    public static void Tick() {
        if (!Paused) {
            _tpsTickCounter++;
            World.Update();

            if (_pauseNextTick) {
                _pauseNextTick = false;
                Pause();
            }
        }
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

        // Update the World's Pixels based on the Engine's PixelBuffer
        foreach (KeyValuePair<Vector2i, Pixel> P in PixelBuffer) { World.Set(P.Key, P.Value); }
        PixelBuffer.Clear();

        if (_halted) { return; }

        // Update Timers
        float DT = GetFrameTime();
        _tpsUpdateTimer += DT;

        for (int i = Timers.Count - 1; i >= 0; i--) {
            Timer T = Timers[i];
            T.Tick(DT);

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
            string FPSText = $"FPS: {GetFPS()}";
            Vector2 Offset = MeasureTextEx(Canvas.DefaultFont, FPSText, Canvas.DefaultFontSize, Canvas.DefaultFontSpacing);
            Canvas.DrawText(FPSText, Config.Resolution.X - (int)Offset.X - 10, 10);
        }

        if (Config.ShowTPS) {
            string TPSText = $"TPS: {_actualTPS}";
            Vector2 Offset = MeasureTextEx(Canvas.DefaultFont, TPSText, Canvas.DefaultFontSize, Canvas.DefaultFontSpacing);
            Canvas.DrawText(TPSText, Config.Resolution.X - (int)Offset.X - 10, 32);
        }
    }

    // Exit safely
    public static void Quit() {
        ShouldQuit = true;
    }
}
