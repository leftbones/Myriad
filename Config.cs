using Calcium;

namespace Myriad;

static class Config {
    // Window Properties
    public static Vector2i WindowSize = new(800, 600);
    public static bool Fullscreen = false;

    // Simulation Settings
    public static bool MultithreadingEnabled = false;

    // Debug Settings
    public static bool VerboseLogging = true;

    public static void Init() {
        Pepper.Log("Configuration loaded", LogType.System);
    }
}