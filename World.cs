using System.Numerics;
using Calcium;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Myriad;

class World {
    public Vector2i Size { get; private set; }

    public Pixel[] Pixels { get; private set; }
    public Chunk[] Chunks { get; private set; }

    public int Tick { get; private set; }

    private Image _buffer;
    private Texture2D _texture;

    private Rectangle _sourceRect;
    private Rectangle _destRect;

    private int _chunkSize;
    private int _maxChunksX;
    private int _maxChunksY;

    public World(int width, int height) {
        Size = new Vector2i(width, height);

        // Image buffer setup
        _buffer = GenImageColor(Size.X, Size.Y, Color.Black);
        _texture = LoadTextureFromImage(_buffer);

        _sourceRect = new Rectangle(0, 0, Size.X, Size.Y);
        _destRect = new Rectangle(0, 0, Config.WindowSize.X, Config.WindowSize.Y);

        // Chunk/matrix setup
        _chunkSize = 50;
        _maxChunksX = Size.X / _chunkSize;
        _maxChunksY = Size.Y / _chunkSize;

        if (Config.VerboseLogging) { Pepper.Log("Building chunks...", LogType.World); }
        Chunks = new Chunk[_maxChunksX * _maxChunksY];
        for(int x = 0; x < _maxChunksX; x++) {
            for(int y = 0; y < _maxChunksY; y++) {
                Chunks[x + y * _maxChunksX] = new Chunk(new Vector2i(x * _chunkSize, y * _chunkSize), new Vector2i(_chunkSize, _chunkSize));
            }
        }

        if (Config.VerboseLogging) { Pepper.Log("Populating pixel matrix...", LogType.World); }
        Pixels = new Pixel[Size.X * Size.Y];
        for (int y = Size.Y - 1; y >= 0; y--) {
            for (int x = 0; x < Size.X; x++) {
                Pixels[x + y * Size.X] = new Pixel(RNG.Odds(10) ? 0 : -1);
            }
        }

        Pepper.Log("World initialized", LogType.System);
    }

    // Check if a position in the World is in bounds
    public bool InBounds(int x, int y) { return InBounds(new Vector2i(x, y)); }
    public bool InBounds(Vector2i pos) {
        return pos.X >= 0 && pos.X < Size.X && pos.Y >= 0 && pos.Y < Size.Y;
    }

    // Check if a position in the World is empty
    public bool IsEmpty(int x, int y) { return IsEmpty(new Vector2i(x, y)); }
    public bool IsEmpty(Vector2i pos) {
        return InBounds(pos) && Get(pos).ID == -1;
    }

    // Check if a position in the World is in bounds and empty
    public bool InBoundsAndEmpty(int x, int y) { return InBoundsAndEmpty(new Vector2i(x, y)); }
    public bool InBoundsAndEmpty(Vector2i pos) {
        return InBounds(pos) && IsEmpty(pos);
    }

    // Get a Pixel in the world
    public Pixel Get(int x, int y) { return Get(new Vector2i(x, y));}
    public Pixel Get(Vector2i pos) {
        return Pixels[pos.X + pos.Y * Size.X];
    }

    // Place a Pixel in the World
    public void Set(int x, int y, Pixel pixel) { Set(new Vector2i(x, y), pixel); }
    public void Set(Vector2i pos, Pixel pixel) {
        Pixels[pos.X + pos.Y * Size.X] = pixel;
    }

    // Swap two Pixels in the World
    public bool Swap(int x1, int y1, int x2, int y2) { return Swap(new Vector2i(x1, y1), new Vector2i(x2, y2)); }
    public bool Swap(Vector2i pos1, Vector2i pos2) {
        if (!InBounds(pos2)) {
            return false;
        }

        var P1 = new Pixel(Get(pos1).ID);
        var P2 = Get(pos2);
        Set(pos2, P1);
        Set(pos1, P2);

        return true;
    }

    // Swap two Pixels in the World without checking if the destination is valid
    public bool UnsafeSwap(int x1, int y1, int x2, int y2) { return UnsafeSwap(new Vector2i(x1, y1), new Vector2i(x2, y2));}
    public bool UnsafeSwap(Vector2i pos1, Vector2i pos2) {
        var P1 = Get(pos1);
        var P2 = Get(pos2);
        Set(pos2, P1);
        Set(pos1, P2);

        return true;
    }

    // Performed immediately before the main Update method
    public void UpdateStart() {

    }

    // Main update method
    public void Update() {
        UpdateStart();

        // Update each Pixel in the World
        var IET = Tick % 2 == 0;
        for (int y = Size.Y - 1; y >= 0; y--) {
            for (int x = IET ? 0 : Size.X - 1; IET ? x < Size.X : x >= 0; x += IET ? 1 : -1) {
                var P = Get(x, y);
                if (P.ID > -1 && !P.Updated) {
                    P.Updated = true;
                    var Pos = new Vector2i(x, y);

                    if (IsEmpty(Pos + Direction.Down) && Swap(Pos, Pos + Direction.Down)) { continue; }
                    if (IsEmpty(Pos + Direction.DownLeft) && Swap(Pos, Pos + Direction.DownLeft)) { continue; }
                    if (IsEmpty(Pos + Direction.DownRight) && Swap(Pos, Pos + Direction.DownRight)) { continue; }
                }
            }
        }

        UpdateEnd();
    }

    // Performed immediately after the main Update method
    public void UpdateEnd() {
        Tick++;
    }

    // Draw each visible Pixel in the World to the render texture
    public unsafe void Draw() {
        ImageClearBackground(ref _buffer, Color.Black);

        for (int y = Size.Y - 1; y >= 0; y--) {
            for (int x = 0; x < Size.X; x++) {
                var P = Get(x, y);
                if (P.ID > -1) {
                    ImageDrawPixel(ref _buffer, x, y, Color.White);
                }
            }
        }

        UpdateTexture(_texture, _buffer.Data);
        DrawTexturePro(_texture, _sourceRect, _destRect, new Vector2(0, 0), 0, Color.White);

        // Debug Drawing
        foreach (var C in Chunks) {
            DrawLine(C.Position.X * Global.PixelScale, C.Position.Y * Global.PixelScale, (C.Position.X + C.Size.X) * Global.PixelScale, C.Position.Y * Global.PixelScale, Color.DarkGray);
            DrawLine(C.Position.X * Global.PixelScale, C.Position.Y * Global.PixelScale, C.Position.X * Global.PixelScale, (C.Position.Y + C.Size.Y) * Global.PixelScale, Color.DarkGray);
            Canvas.DrawText($"{C.Position.X / _chunkSize}, {C.Position.Y / _chunkSize}", C.Position.X * Global.PixelScale + 5, C.Position.Y * Global.PixelScale + 5, 8, color: Color.DarkGray);
        }
    }
}