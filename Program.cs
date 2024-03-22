using Raylib_cs;
using static Raylib_cs.Raylib;
using static Raylib_cs.TraceLogLevel;

namespace Myriad;

class Program {
    static void Main(string[] args) {
        // Setup
        SetTraceLogLevel(Warning | Error | Fatal);
        SetExitKey(KeyboardKey.Null);
        InitWindow(Config.WindowSize.X, Config.WindowSize.Y, "Myriad");

        Pepper.Log("Program started successfully", LogType.System);

        Engine.Init();


        // Main Loop
        while (!WindowShouldClose()) {
            // Update
            Engine.Update();

            // Draw
            BeginDrawing();
            ClearBackground(Color.Black);

            Engine.Draw();

            EndDrawing();
        }

        // Exit
        CloseWindow();
        Pepper.Log("Program exited successfully", LogType.System);
    }
}
