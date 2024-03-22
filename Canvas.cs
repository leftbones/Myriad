using System.Numerics;
using Calcium;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Myriad;

enum Anchor { TopLeft, TopRight, BottomLeft, BottomRight, Center }

static class Canvas {
    public static int BrushSize = 4;

    public static Font DefaultFont;
    public static int DefaultFontSize;
    public static int DefaultFontSpacing;
    public static Color DefaultTextColor;

    // Initialize the Canvas
    public static void Init() {
        if (Config.VerboseLogging) { Pepper.Log("Loading fonts...", LogType.System); }

        DefaultFont = LoadFontEx("assets/fonts/Kitchen Sink.ttf", 96, null, 256);
        DefaultFontSize = 16;
        DefaultFontSpacing = 0;
        DefaultTextColor = Color.White;

        Pepper.Log("Canvas initialized", LogType.Other);
    }

    public static void DrawText(string text, int x, int y, int? font_size=null, int? font_spacing=null, Color? color=null, Anchor anchor=Anchor.TopLeft) {
        var FontSize = font_size ?? DefaultFontSize;
        var FontSpacing = font_spacing ?? DefaultFontSpacing;
        var TextColor = color ?? DefaultTextColor;

        var AnchorPos = new Vector2(x, y);
        switch (anchor) {
            case Anchor.TopLeft:
                break;

            case Anchor.TopRight:
                AnchorPos -= new Vector2(MeasureTextEx(DefaultFont, text, FontSize, FontSpacing).X, 0);
                break;

            case Anchor.BottomLeft:
                AnchorPos -= new Vector2(0, MeasureTextEx(DefaultFont, text, FontSize, FontSpacing).Y);
                break;

            case Anchor.BottomRight:
                AnchorPos -= MeasureTextEx(DefaultFont, text, FontSize, FontSpacing);
                break;

            case Anchor.Center:
                AnchorPos -= MeasureTextEx(DefaultFont, text, FontSize, FontSpacing) / 2;
                break;
        }

        DrawTextEx(DefaultFont, text, AnchorPos, FontSize, FontSpacing, TextColor);
    }
}