using System.Drawing;
using Console = Colorful.Console;
using Myriad.Core;

namespace Myriad.Helper;

/// <summary>
/// The in-engine console, named "Pepper" after my daughter because the name "Console" is used already in .NET
/// Currently only handles .NET console output and can be used for throwing exceptions with an in-game window
/// Will be used for creating and managing log files in the future
/// </summary>

public enum LogType { System, Engine, World, Interface, Input, Debug, Other};
public enum LogLevel { Message, Warning, Error, Fatal, Special };

public static class Pepper {
    private static readonly Color MessageColor =     Color.Transparent;
    private static readonly Color WarningColor =     Color.Yellow;
    private static readonly Color ErrorColor =       Color.OrangeRed;
    private static readonly Color FatalColor =       Color.DarkRed;
    private static readonly Color SpecialColor =     Color.Green;
    private static readonly Color DebugColor =       Color.Magenta;

    // Settings
    public static readonly bool LogMessage =         true;
    public static readonly bool LogWarning =         true;
    public static readonly bool LogError =           true;
    public static readonly bool LogFatal =           true;
    public static readonly bool LogSpecial =         true;
    public static readonly bool LogDebug =           true;


    // Generate the current timestamp (for logs)
    public static string Timestamp(int type=0) {
        if (type == 0) {
            return DateTime.Now.ToString("[HH:mm:ss]", System.Globalization.CultureInfo.InvariantCulture);             // Log write format
        } else if (type == 1) {
            return DateTime.Now.ToString("yyyy.MM.dd_HH.mm.ss", System.Globalization.CultureInfo.InvariantCulture);    // File name format
        }

        Log("Invalid Timestamp type passed to Pepper.Timestamp (requires 0-1)", LogType.Other, LogLevel.Error);
        return "TimeIsNotReal";
    }

    // Throw an exception and write the exception to the current log file
    public static void Yell(string message, LogType type=LogType.Debug, LogLevel level=LogLevel.Fatal) { Throw(message, type, level); }
    public static void Throw(string message, LogType type=LogType.Debug, LogLevel level=LogLevel.Fatal) {
        Log(message, type, level);
        Engine.Halt();
    }

    // Log a warning to the console as well as the current log file
    public static void Scold(string message, LogType type) { Warn(message, type); }
    public static void Warn(string message, LogType type) {
        Log(message, type, LogLevel.Warning);
    }

    // Log a message to the console as well as the current log file
    public static void Sing(string message, LogType type=LogType.Debug, LogLevel level=LogLevel.Message) { Log(message, type, level); }
    public static void Log(string message, LogType type=LogType.Debug, LogLevel level=LogLevel.Message) {
        if (level == LogLevel.Message && !LogMessage) { return; }
        else if (level == LogLevel.Warning && !LogWarning) { return; }
        else if (level == LogLevel.Error && !LogError) { return; }
        else if (level == LogLevel.Fatal && !LogFatal) { return; }
        else if (level == LogLevel.Special && !LogSpecial) { return; }

        Color Col = MessageColor;
        switch (level) {
            case LogLevel.Message: Col = MessageColor; break;
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

        string Entry = $"{Timestamp()} [{type.ToString().ToUpper(System.Globalization.CultureInfo.InvariantCulture)}] {level}: {message}";
        Console.WriteLine(Entry, Col);
    }
}
