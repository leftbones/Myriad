using System.Drawing;
using Console = Colorful.Console;

namespace Myriad;

/// <summary>
/// The in-engine console, named "Pepper" after my daughter because the name "Console" is used already in .NET
/// Currently only handles .NET console output and can be used for throwing exceptions with an in-game window
/// Will be used for creating and managing log files in the future
/// </summary>

enum LogType { System, Engine, World, Interface, Input, Debug, Other};
enum LogLevel { Message, Warning, Error, Fatal, Special };

static class Pepper {
    private static Color MessageColor =     Color.Transparent;
    private static Color WarningColor =     Color.Yellow;
    private static Color ErrorColor =       Color.OrangeRed;
    private static Color FatalColor =       Color.DarkRed;
    private static Color SpecialColor =     Color.Green;
    private static Color DebugColor =       Color.Magenta;

    // Settings
    public static bool LogMessage =         true;
    public static bool LogWarning =         true;
    public static bool LogError =           true;
    public static bool LogFatal =           true;
    public static bool LogSpecial =         true;
    public static bool LogDebug =           true;


    // Generate the current timestamp (for logs)
    public static string Timestamp(int type=0) {
        if (type == 0) {
            return DateTime.Now.ToString("[HH:mm:ss]");             // Log write format
        } else if (type == 1) {
            return DateTime.Now.ToString("yyyy.MM.dd_HH.mm.ss");    // File name format
        }

        Log("Invalid Timestamp type passed to Pepper.Timestamp (requires 0-1)", LogType.Other, LogLevel.Error);
        return "TimeIsNotReal";
    }

    // Throw an exception and write the exception to the current log file
    public static void Yell(string message, LogType type=LogType.Debug, LogLevel level=LogLevel.Fatal) { Throw(message, type, level); }
    public static void Throw(string message, LogType type=LogType.Debug, LogLevel level=LogLevel.Fatal) {
        Log(message, type, level);
        // Engine.Halt();
    }

    // Log a message to the console as well as the current log file
    public static void Sing(string message, LogType type=LogType.Other, LogLevel level=LogLevel.Message) { Log(message, type, level); }
    public static void Log(string message, LogType type=LogType.Other, LogLevel level=LogLevel.Message) {
        if (level == LogLevel.Message && !LogMessage) { return; }
        else if (level == LogLevel.Warning && !LogWarning) { return; }
        else if (level == LogLevel.Error && !LogError) { return; }
        else if (level == LogLevel.Fatal && !LogFatal) { return; }
        else if (level == LogLevel.Special && !LogSpecial) { return; }

        var Col = MessageColor;
        switch(level) {
            case LogLevel.Warning: Col = WarningColor; break;
            case LogLevel.Error: Col = ErrorColor; break;
            case LogLevel.Fatal: Col = FatalColor; break;
            case LogLevel.Special: Col = SpecialColor; break;
            default: break;
        }

        // Always use DebugColor for Debug logs
        if (type is LogType.Debug) {
            Col = DebugColor;
        }

        var Entry = $"{Timestamp()} [{type.ToString().ToUpper()}] {level}: {message}";
        Console.WriteLine(Entry, Col);
    }
}
