using System.Diagnostics;
using rlImGui_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static Raylib_cs.TraceLogLevel;

namespace Myriad;

class Program {
    static void Main(string[] args) {
        // Window Setup
        SetTraceLogLevel(Warning | Error | Fatal);
        SetExitKey(KeyboardKey.Null);
        InitWindow(Config.Resolution.X, Config.Resolution.Y, "Myriad");

        rlImGui.Setup();

        // System Setup
        Engine.Init();

        var SW = new Stopwatch();
        SW.Start();

        double totalTime = 0.0;
        long previousTime = SW.ElapsedMilliseconds;
        long lastSecondTime = 0;
        int tickCount = 0;

        // Finish
        Pepper.Log("Initialization complete", LogType.System);

        // Main Loop
        while (!WindowShouldClose()) {
            // Timing
            if (!Engine.Paused) {
                long currentTime = SW.ElapsedMilliseconds;
                long elapsedTime = currentTime - previousTime;
                previousTime = currentTime;
                totalTime += elapsedTime;

                if (currentTime - lastSecondTime >= 1000) {
                    lastSecondTime = currentTime;
                    tickCount = 0;
                }
            } else {
                previousTime = SW.ElapsedMilliseconds;
            }


            //
            // Update
            Engine.Update();

            // Tick
            int ticksThisFrame = 0;
            while (totalTime >= Engine.TickInterval && ticksThisFrame < Engine.MaxTicksPerFrame) {
                Engine.Tick();
                totalTime -= Engine.TickInterval;
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
