using System.Numerics;
using Calcium;
using rlImGui_cs;
using ImGuiNET;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Myriad;

static class Engine {
    public static World World { get; private set; }
    public static int TicksPerSecond = 200;
    public static double TickRateMultiplier = 1.0;

    public static List<Timer> Timers = [];
    private static Timer _tickTimer;

    private static bool _halted = false;

    // Initialize all engine components
    public static void Init() {
        Config.Init();
        Canvas.Init();
        Materials.Init();

        World = new World(Config.WindowSize.X / Global.PixelScale, Config.WindowSize.Y / Global.PixelScale);
        Pepper.Log("Engine initialized", LogType.System);

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
        var MousePos = new Vector2i(
            (int)Math.Round((double)GetMouseX() / Global.PixelScale),
            (int)Math.Round((double)GetMouseY() / Global.PixelScale)
        );

        if (ImGui.GetIO().WantCaptureMouse) { return; }

        if (IsKeyDown(KeyboardKey.LeftShift)) {
            if (IsMouseButtonPressed(MouseButton.Left) && World.InBounds(MousePos)) {
                World.MakeExplosion(MousePos, Canvas.BrushSize * 10);
            }
        } else {
            if (IsMouseButtonDown(MouseButton.Left) && World.InBounds(MousePos)) {
                for (int x = -Canvas.BrushSize; x <= Canvas.BrushSize; x++) {
                    for (int y = -Canvas.BrushSize; y <= Canvas.BrushSize; y++) {
                        if (RNG.Chance(2) && World.InBounds(MousePos + new Vector2i(x, y))) {
                            if (World.IsEmpty(MousePos + new Vector2i(x, y))) {
                                var P = Materials.New(Canvas.BrushMaterial);
                                World.Set(MousePos + new Vector2i(x, y), P);
                            }
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
        World.Draw();

        rlImGui.Begin();

        ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.Appearing);
        ImGui.SetNextWindowSize(new Vector2(350, 400), ImGuiCond.Appearing);
        if (ImGui.Begin($"Debug")) {
            if (ImGui.CollapsingHeader("Info", ImGuiTreeNodeFlags.DefaultOpen)) {
                ImGui.Text($"FPS: {GetFPS()}");
                ImGui.Text($"Pixels: {World.PixelCount}");
                ImGui.Text($"Particles: {World.ParticleCount}");
            }

            if (ImGui.CollapsingHeader("Brush", ImGuiTreeNodeFlags.DefaultOpen)) {
                if (ImGui.Button("Mode")) { Canvas.BrushMode = (Canvas.BrushMode + 1) % 2; }
                ImGui.SameLine(); ImGui.Text(Canvas.BrushMode == 0 ? "Paint" : "Erase");

                if (ImGui.Button("Type")) { Canvas.BrushType = (Canvas.BrushType + 1) % 2; }
                ImGui.SameLine(); ImGui.Text(Canvas.BrushType == 0 ? "Brush" : "Rectangle");

                if (Canvas.BrushType == 0) { ImGui.SliderInt("Size", ref Canvas.BrushSize, 1, 16); }
                ImGui.Combo("Material", ref Canvas.BrushMaterialID, [.. Materials.ByID], Materials.ByID.Count);
            }

            if (ImGui.CollapsingHeader("Simulation", ImGuiTreeNodeFlags.DefaultOpen)) {
                ImGui.InputInt("Speed", ref TicksPerSecond);
                ImGui.SameLine(); if (ImGui.Button("Apply")) { SetTicksPerSecond(TicksPerSecond); }
                ImGui.Checkbox("Multithreading", ref Config.MultithreadingEnabled);
            }

            if (ImGui.CollapsingHeader("Overlays", ImGuiTreeNodeFlags.DefaultOpen)) {
                ImGui.Checkbox("Chunk borders", ref Config.DrawChunkBorders);
                ImGui.Checkbox("Update rects", ref Config.DrawUpdateRects);
            }

            if (ImGui.CollapsingHeader("Commands", ImGuiTreeNodeFlags.DefaultOpen)) {
                if (ImGui.Button("Clear")) { Cheats.FillWorld(World, "air"); }
                ImGui.SameLine(); if (ImGui.Button("Fill")) { Cheats.FillWorld(World, Canvas.BrushMaterial); }
                ImGui.SameLine(); if (ImGui.Button("NUKE!")) { Cheats.NukeWorld(World); }

                if (ImGui.Button("Reload Materials")) { Materials.Reload(); };
            }
        }

        ImGui.End();

        rlImGui.End();
    }
}