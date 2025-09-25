using System.Diagnostics;
using rlImGui_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static Raylib_cs.TraceLogLevel;
using Myriad.Core;
using Myriad.Helper;

namespace Myriad;

public class Program {
    public static void Main(string[] args) {
        // Window Setup
        SetTraceLogLevel(Warning | Error | Fatal);
        SetExitKey(KeyboardKey.Null);
        InitWindow(Config.Resolution.X, Config.Resolution.Y, "Myriad");

        rlImGui.Setup();

        // System Setup
        Engine.Init();

        Stopwatch SW = new Stopwatch();
        SW.Start();

        long totalTimeTicks = 0;
        long previousTimeTicks = SW.Elapsed.Ticks;
        long lastSecondTime = 0;
        int tickCount = 0;

        // Finish
        Pepper.Log("Initialization complete", LogType.System);

        // Main Loop
        while (!WindowShouldClose()) {
            // Timing
            long currentTimeTicks = SW.Elapsed.Ticks;
            long currentTimeMs = SW.ElapsedMilliseconds;

            if (Engine.TimingReset) {
                totalTimeTicks = 0;
                previousTimeTicks = currentTimeTicks;
                Engine.TimingReset = false;
            } else if (!Engine.Paused) {
                long elapsedTicks = currentTimeTicks - previousTimeTicks;
                totalTimeTicks += elapsedTicks;
            }

            previousTimeTicks = currentTimeTicks;

            if (currentTimeMs - lastSecondTime >= 1000) {
                lastSecondTime = currentTimeMs;
                tickCount = 0;
            }


            //
            // Update
            Engine.Update();

            // Tick
            int ticksThisFrame = 0;
            while ((totalTimeTicks >= Engine.TickIntervalTicks && ticksThisFrame < Engine.MaxTicksPerFrame)) {
                Engine.Tick();
                totalTimeTicks -= Engine.TickIntervalTicks;
                ticksThisFrame++;
                tickCount++;
            }


            //
            // Draw
            BeginDrawing();
                Engine.Draw();
            EndDrawing();

            if (Engine.ShouldQuit) {
                break;
            }
        }

        //
        // Exit
        CloseWindow();
        rlImGui.Shutdown();
        Pepper.Log("Program exited successfully", LogType.System);
    }
}
