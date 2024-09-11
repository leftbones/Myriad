using rlImGui_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static Raylib_cs.TraceLogLevel;

namespace Myriad;

class Program {
    static void Main(string[] args) {
        // Setup
        SetTraceLogLevel(Warning | Error | Fatal);
        SetExitKey(KeyboardKey.Null);
        InitWindow(Config.Resolution.X, Config.Resolution.Y, "Myriad");

        rlImGui.Setup();

        Pepper.Log("Program started successfully", LogType.System);

        Engine.Init();


        // Main Loop
        while (!WindowShouldClose()) {
            // Update
            Engine.Update();

            // Draw
            BeginDrawing();

            Engine.Draw();

            EndDrawing();

            if (Engine.ShouldQuit) {
                break;
            }
        }

        // Exit
        CloseWindow();
        rlImGui.Shutdown();
        Pepper.Log("Program exited successfully", LogType.System);
    }
}
