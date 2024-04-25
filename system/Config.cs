using Calcium;

namespace Myriad;

static class Config {
    // Window Properties
    public static Vector2i WindowSize = new(800, 600);
    public static bool Fullscreen = false;

    // Simulation Settings
    public static bool MultithreadingEnabled = true;

    // Debug Settings
    public static bool DebugMode = true;
    public static bool VerboseLogging = true;
    public static bool DrawChunkBorders = false;
    public static bool DrawUpdateRects = false;

    public static void Init() {
        Pepper.Log("Configuration loaded", LogType.System);

        if (MultithreadingEnabled) {
            Pepper.Log("Experimental multithreading is enabled!", LogType.System, LogLevel.Warning);
        }
    }
}