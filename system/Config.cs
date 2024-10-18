using Calcium;

namespace Myriad;

static class Config {
    // Window Properties
    public static Vector2i Resolution = new(1200, 800);
    public static bool Fullscreen = false;
    public static bool SystemCursor = false;

    // Simulation Settings
    public static bool ShowFPS = true;
    public static bool ShowTPS = true;
    public static bool Multithreading = true;

    // Debug Settings
    public static bool DebugMode = true;
    public static bool VerboseLogging = true;
    public static bool DrawChunkBorders = false;
    public static bool DrawUpdateRects = false;

    public static void Init() {
        Pepper.Log("Configuration loaded", LogType.System);

        if (Multithreading) {
            Pepper.Warn("Experimental multithreading is enabled!", LogType.Engine);
        }
    }
}
