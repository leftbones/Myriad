using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Myriad;

enum Anchor { TopLeft, TopRight, BottomLeft, BottomRight, Center }

static class Canvas {
    public static int BrushSize = 4;
    public static int BrushMaterialID = Materials.ByID.IndexOf("sand");
    public static string BrushMaterial => Materials.ByID[BrushMaterialID];
    public static int BrushMode = 0;
    public static int BrushType = 0;
    public static bool PaintOnTop = false;

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

    // Draw text to the screen using the Canvas font and settings, allows for usage of anchors for better positioning
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

    // Shift all values of a color by a certain amount
    public static Color ShiftColor(Color color, int amount) {
        return new Color(
            (byte)Math.Clamp(color.R + amount, 0, 255),
            (byte)Math.Clamp(color.G + amount, 0, 255),
            (byte)Math.Clamp(color.B + amount, 0, 255),
            color.A
        );
    }

    // Blend two colors together by a specified amount
    public static Color BlendColor(Color a, Color b, float amount=0.5f) {
        return new Color(
            (byte)(a.R * (1 - amount) + (b.R * amount)),
            (byte)(a.G * (1 - amount) + (b.G * amount)),
            (byte)(a.B * (1 - amount) + (b.B * amount)),
            a.A
        );
    }

    // Return a Color from a string hex code
    public static Color HexColor(string color_hex) {
        var color_int = (uint)Convert.ToInt32(color_hex, 16);
        return GetColor(color_int);
    }
}
