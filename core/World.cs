using System.Numerics;
using Calcium;
using Myriad.Helper;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Myriad.Core;

// TODO: Maybe make a ChunkHandler class that will handle all chunk operations, similar to the PixelHandler I want to make, this file is getting long

internal class World {
    public Vector2i Size { get; private set; }

    public Pixel[] Pixels { get; private set; }
    public Chunk[] Chunks { get; private set; }

    public List<Chunk> ActiveChunks { get; }

    public List<Particle> Particles { get; private set; }

    public int Tick { get; private set; }
    public int PixelCount => Pixels.Count(static P => P.ID != "air");  // FIXME: THIS IS VERY SLOW
    public int ParticleCount => Particles.Count;    					// FIXME: THIS IS ALSO PRETTY SLOW

    public bool ProcessDone { get; set; }

    private Image _buffer;
    private Texture2D _texture;

    private Rectangle _sourceRect;
    private Rectangle _destRect;

    private readonly int _maxChunksX;
    private readonly int _maxChunksY;

    public World(int width, int height) {
        Size = new Vector2i(width, height);

        ActiveChunks = [];

        // Image buffer setup
        _buffer = GenImageColor(Size.X, Size.Y, Color.Black);
        _texture = LoadTextureFromImage(_buffer);

        _sourceRect = new Rectangle(0, 0, Size.X, Size.Y);
        _destRect = new Rectangle(0, 0, Config.Resolution.X, Config.Resolution.Y);

        // Chunk/matrix setup
        _maxChunksX = Size.X / Global.ChunkSize;
        _maxChunksY = Size.Y / Global.ChunkSize;

        if (Config.VerboseLogging) { Pepper.Log("Building chunks...", LogType.World); }
        Chunks = new Chunk[_maxChunksX * _maxChunksY];
        for(int x = 0; x < _maxChunksX; x++) {
            for(int y = 0; y < _maxChunksY; y++) {
                int ThreadOrder = y % 2 == 0 ? x % 2 == 0 ? 1 : 2 : x % 2 == 0 ? 3 : 4;
                Chunks[x + (y * _maxChunksX)] = new Chunk(new Vector2i(x * Global.ChunkSize, y * Global.ChunkSize), ThreadOrder);
            }
        }

        if (Config.VerboseLogging) { Pepper.Log("Populating pixel matrix...", LogType.World); }
        Pixels = new Pixel[Size.X * Size.Y];
        for (int y = Size.Y - 1; y >= 0; y--) {
            for (int x = 0; x < Size.X; x++) {
                Pixels[x + (y * Size.X)] = new Pixel();
            }
        }

        // Other
        Particles = [];

        Pepper.Log("World initialized", LogType.System);
    }

    // FIXME: Kinda broken? Definitely needs work
    // Create an explosion at a position in the World
    public void MakeExplosion(int x, int y, int radius) { MakeExplosion(new Vector2i(x, y), radius); }
    public void MakeExplosion(Vector2i pos, int radius) {
            List<Vector2i> CirclePoints = Algorithm.GetCirclePoints(pos, radius);
            List<Vector2i> PointCache = [];

            foreach (Vector2i CP in CirclePoints) {
                foreach (Vector2i PT in Algorithm.GetLinePoints(pos, CP)) {
                    if (PointCache.Contains(PT)) {
                        continue;
                    }

                    PointCache.Add(PT);

                    if (!InBounds(PT)) {
                        continue;
                    }

                    Pixel P = Get(PT);
                    if (P.ID == "air") {
                        if (RNG.Chance(25)) {
                            Pixel Fire = Materials.New("fire");
                            Fire.Color = Global.FireColors[RNG.Range(0, 2)];
                            Set(PT, Fire);
                        }
                        continue;
                    }

                    // TODO: Reduce damage done by the explosion further away from the center
                    P.Damage(1);

                    if (P.Health is > 0 or -1) {
                        Material M = Materials.Index[P.ID];
                        if (M.Type is "powder" or "liquid") {
                            P.Stain(new Color(25, 25, 25, 255), 0.1f);
                            Particle Particle = new Particle(PT.ToVec2(), P.Color, P);
                            Particle.ApplyForce(new Vector2(RNG.Roll(2.5f) * RNG.Range(-1, 1), RNG.Roll(3.5f) * -1));
                            Particles.Add(Particle);
                            Set(PT, Materials.New("air"));
                        }
                    }
                }
            }
        }

    // Check if a position in the World is in bounds
    public bool InBounds(int x, int y) { return InBounds(new Vector2i(x, y)); }
    public bool InBounds(Vector2i pos) {
        return pos.X >= 0 && pos.X < Size.X && pos.Y >= 0 && pos.Y < Size.Y;
    }

    // Check if a position in the World is empty
    public bool IsEmpty(int x, int y) { return IsEmpty(new Vector2i(x, y)); }
    public bool IsEmpty(Vector2i pos) {
        return InBounds(pos) && Get(pos).ID == "air";
    }

    // Check if a position in the World is in bounds and empty
    public bool InBoundsAndEmpty(int x, int y) { return InBoundsAndEmpty(new Vector2i(x, y)); }
    public bool InBoundsAndEmpty(Vector2i pos) {
        return InBounds(pos) && IsEmpty(pos);
    }

    // Get a Pixel in the world
    public Pixel Get(int x, int y) { return Get(new Vector2i(x, y)); }
    public Pixel Get(Vector2i pos) {
        return Pixels[pos.X + (pos.Y * Size.X)];
    }

    // Quick access to material data by position
    public string GetID(Vector2i pos) { return Get(pos).ID; }
    public string GetName(Vector2i pos) { return Materials.Index[GetID(pos)].Name; }
    public string GetType(Vector2i pos) { return Materials.Index[GetID(pos)].Type; }

    // Place a Pixel in the World
    public void Set(int x, int y, Pixel pixel, bool wake_chunk=true) { Set(new Vector2i(x, y), pixel, wake_chunk); }
    public void Set(Vector2i pos, Pixel pixel, bool wake_chunk=true) {
        Pixels[pos.X + (pos.Y * Size.X)] = pixel;
        if (wake_chunk) { WakeChunk(pos); }
    }

    // Swap two Pixels in the World
    public bool Swap(int x1, int y1, int x2, int y2) { return Swap(new Vector2i(x1, y1), new Vector2i(x2, y2)); }
    public bool Swap(Vector2i pos1, Vector2i pos2) {
        if (!InBounds(pos2)) { return false; }
        UnsafeSwap(pos1, pos2);

        return true;
    }

    // Swap two Pixels in the World without checking if the destination is in bounds
    public bool UnsafeSwap(int x1, int y1, int x2, int y2) { return UnsafeSwap(new Vector2i(x1, y1), new Vector2i(x2, y2)); }
    public bool UnsafeSwap(Vector2i pos1, Vector2i pos2) {
        Pixel P1 = Get(pos1);
        Pixel P2 = Get(pos2);
        Set(pos2, P1);
        Set(pos1, P2);

        return true;
    }

    // Swap two Pixels in the World if the destination is in bounds and the density values allow a swap
    public bool ValidSwap(int x1, int y1, int x2, int y2) { return ValidSwap(new Vector2i(x1, y1), new Vector2i(x2, y2)); }
    public bool ValidSwap(Vector2i pos1, Vector2i pos2) {
        if (!InBounds(pos2)) { return false; }
        Pixel P1 = Get(pos1);
        Pixel P2 = Get(pos2);
        Material M1 = Materials.Get(P1.ID);
        Material M2 = Materials.Get(P2.ID);

        if (M1.Type == "gas" && M2.Type == "solid") { return false; }

        if (M1.Density > 0) {
            if (M1.Density > M2.Density) { return UnsafeSwap(pos1, pos2); }
        }
        else {
            if (M1.Density < M2.Density) { return UnsafeSwap(pos1, pos2); }
        }

        return false;
    }


    // Try to perform reactions between materials
    public bool CanReact(Material M, Vector2i Pos, Vector2i Dir) {
        if (InBounds(Pos + Dir)) {
            Pixel P = Get(Pos);
            Pixel NP = Get(Pos + Dir);

            // Check for a reaction between this pixel's material and it's neighbor's material
            Reaction Reaction = M.Reactions.Find(R => R.Reactant == NP.ID);
            if (Reaction == null) { return false; }
            if (RNG.Odds(Reaction.Chance)) {
                // Pixel
                if (Materials.IsStatus(Reaction.Products.Item1)) {
                    switch (Reaction.Products.Item1) {
                        case "<burning>":
                            P.Burning = true;
                            break;
                        default:
                            break;
                    }
                }
                else {
                    Set(Pos, Materials.New(Reaction.Products.Item1));
                }

                // Neighbor
                if (Materials.IsStatus(Reaction.Products.Item2)) {
                    switch (Reaction.Products.Item2) {
                        case "<burning>":
                            NP.Burning = true;
                            break;
                        default:
                        	break;
                    }
                }
                else {
                    Set(Pos + Dir, Materials.New(Reaction.Products.Item2));
                }

                return true;
            }
        }

        // No reactions occurred
        return false;
    }

    // Get a Chunk from a position in the World
    public Chunk GetChunk(int x, int y) { return GetChunk(new Vector2i(x, y)); }
    public Chunk GetChunk(Vector2i pos) {
            return Chunks[(pos.X / Global.ChunkSize) + (pos.Y / Global.ChunkSize * _maxChunksX)];
        }

    // Wake a Chunk from a position in the World
    public void WakeChunk(Vector2i pos) {
        Chunk Chunk = GetChunk(pos);

        if (!ActiveChunks.Contains(Chunk)) {
            ActiveChunks.Add(Chunk);
        }

        Chunk.Wake(pos); // This is run even if the chunk was already awake because it resets the chunk's sleep timer

        // Wake appropriate neighbor chunks if the position is on a border (only if this chunk was not awake the previous tick)
        if (pos.X == Chunk.Position.X + Global.ChunkSize - 1 && InBounds(pos + Direction.Right)) { GetChunk(pos + Direction.Right).Wake(pos + Direction.Right); }
        if (pos.X == Chunk.Position.X && InBounds(pos + Direction.Left)) { GetChunk(pos + Direction.Left).Wake(pos + Direction.Left); }
        if (pos.Y == Chunk.Position.Y + Global.ChunkSize - 1 && InBounds(pos + Direction.Down)) { GetChunk(pos + Direction.Down).Wake(pos + Direction.Down); }
        if (pos.Y == Chunk.Position.Y && InBounds(pos + Direction.Up)) { GetChunk(pos + Direction.Up).Wake(pos + Direction.Up); }
    }

    // Performed immediately before the main Update method
    public void UpdateStart() {
        // Reset the processing flag
        ProcessDone = false;

        // Set all Pixels to not updated (NOTE: IET stands for 'Is Even Tick')
        bool IET = Tick % 2 == 0;
        foreach (Chunk Chunk in Chunks) {
            int X1 = Chunk.Position.X + Chunk.X1;
            int Y1 = Chunk.Position.Y + Chunk.Y1;
            int X2 = Chunk.Position.X + Chunk.X2;
            int Y2 = Chunk.Position.Y + Chunk.Y2;

            for (int y = Y1; y <= Y2; y++) {
                for (int x = IET ? X1 : X2; IET ? x <= X2 : x >= X1; x += IET ? 1 : -1) {
                    Pixel P = Get(x, y);
                    if (P.ID != "air") {
                        P.Updated = false;
                        P.Lifetime++;

                        // Remove dead
                        if (P.Lifespan > 0 && P.Lifetime >= P.Lifespan) {
                            Set(x, y, Materials.New("air"));
                        }

                        // Remove damaged
                        if (P.Health == 0) {
                            Set(x, y, Materials.New("air"));
                        }
                    }
                }
            }
        }

        // Update all Particles, turning them into Pixels if they collide with an existing Pixel
        for (int i = Particles.Count - 1; i >= 0; i--) {
            Particle P = Particles[i];
            P.Tick();

            Vector2i WorldPos = new Vector2i(P.Position);
            if (P.Lifespan > 0 && P.Lifetime >= P.Lifespan) {
                Particles.RemoveAt(i);
                Set(WorldPos, P.ExpirePixel);
                continue;
            }

            P.ApplyForce(P.GravityDir * Global.ParticleGravity);
            P.Position += P.Velocity;

            if (!InBounds(WorldPos)) {
                // Particle is out of bounds
                Particles.RemoveAt(i);
                continue;
            }
            else {
                // Particle is inside of a Pixel
                if (!IsEmpty(WorldPos)) {
                    Particles.RemoveAt(i);
                    continue;
                }

                // Particle can move freely
                Vector2i ProjPos = new Vector2i(P.Position + P.Velocity);
                List<Vector2i> LinePoints = Algorithm.GetLinePoints(WorldPos, ProjPos);

                if (P.Grace == 0) {	// Particle is not in it's grace period
                    for (int j = 0; j < LinePoints.Count; j++) {
                        Vector2i Next = LinePoints[(j + 1) % LinePoints.Count];

                        if (InBounds(Next)) {
                            Pixel Pixel = Get(Next);
                            if (Pixel.ID != "air") { // Particle collided with a Pixel
                                Particles.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    // Main update method
    public void Update() {
        UpdateStart();

        // Update each Pixel in the World
        if (Config.Multithreading) {
            // ThreadedUpdate();
            ParallelUpdate();
        }
        else {
            foreach (Chunk Chunk in Chunks) {
                ProcessChunk(Chunk);
                Chunk.Step();
            }
        }

        UpdateEnd();
    }

    // Update in multiple threads (old method)
    // public void ThreadedUpdate() {
    //     IOrderedEnumerable<IGrouping<int, Chunk>> ChunkGroups = Chunks.GroupBy(C => C.ThreadOrder).OrderBy(G => G.Key);

    //     foreach (IGrouping<int, Chunk> Group in ChunkGroups) {
    //         Chunk[] ChunksArray = [.. Group];
    //         Thread[] threads = new Thread[ChunksArray.Length];

    //         for (int i = 0; i < ChunksArray.Length; i++) {
    //             Chunk chunk = ChunksArray[i];
    //             threads[i] = new Thread(() => {
    //                 ProcessChunk(chunk);
    //                 chunk.Step();
    //             });
    //             threads[i].Start();
    //         }

    //         for (int i = 0; i < threads.Length; i++) {
    //             threads[i].Join();
    //         }
    //     }
    // }

    // Update in parallel (new method, seems faster)
    public void ParallelUpdate() {
        IOrderedEnumerable<IGrouping<int, Chunk>> ChunkGroups = Chunks.GroupBy(C => C.ThreadOrder).OrderBy(G => G.Key); // Should be updating only active chunks I think?

        foreach (IGrouping<int, Chunk> Group in ChunkGroups) {
            Chunk[] ChunksArray = [.. Group];
            Parallel.ForEach(ChunksArray, chunk => {
                ProcessChunk(chunk);
                chunk.Step();
            });
        }
    }

    // Performed immediately after the main Update method
    public void UpdateEnd() {
        // Remove sleeping chunks from the list of active chunks
        ActiveChunks.RemoveAll(static chunk => !chunk.Awake);

        // Increment the tick counter
        Tick++;
    }

    // Process all Pixels in a Chunk
    public void ProcessChunk(Chunk chunk) {
        // if (!chunk.Awake) {
        //     return;
        // }

        bool IET = Tick % 2 == 0;

        int X1 = chunk.Position.X + chunk.X1;
        int Y1 = chunk.Position.Y + chunk.Y1;
        int X2 = chunk.Position.X + chunk.X2;
        int Y2 = chunk.Position.Y + chunk.Y2;

        for (int y = Y1; y <= Y2; y++) {
            for (int x = IET ? X1 : X2; IET ? x <= X2 : x >= X1; x += IET ? 1 : -1) {
                Pixel P = Get(x, y);
                if (P.ID == "air" || P.Updated) { continue; }

                Material M = Materials.Index[P.ID];
                Vector2i Pos = new Vector2i(x, y);

                P.Updated = true;

                // Status Conditions
                if (P.Burning) {
                    if (P.BurnTimer >= P.TimeToBurn) {
                        Set(Pos, Materials.New("fire"));
                        continue;
                    }
                    else {
                        P.BurnTimer++;

                        if (RNG.Chance(5)) {
                            foreach (Vector2i Dir in Direction.Shuffled(Direction.Full)) {
                                if (InBounds(Pos + Dir)) {
                                    Pixel NP = Get(Pos + Dir);
                                    if (Materials.HasTag(NP.ID, "flammable") && !NP.Burning && RNG.Odds(10)) {
                                        Set(Pos + Dir, Materials.New("fire"));
                                        break;
                                    }
                                }
                            }
                        }
                        else {
                            WakeChunk(Pos);
                        }
                    }
                }

                // Behavior
                switch (M.Type) {
                    case "solid":
                        continue;

                    case "powder":
                        Materials.TickPowder(this, P, Pos);
                        break;

                    case "liquid":
                        Materials.TickLiquid(this, P, Pos);
                        break;

                    case "gas":
                        Materials.TickGas(this, P, Pos);
                        break;

                    default:
                        break;
                }
            }
        }

        // Reactions + Interactions (NOTE: What did I mean by "Interactions"? I can't remember and I can't figure it out.)
        for (int y = Y2; y >= Y1; y--) {
            for (int x = IET ? X1 : X2; IET ? x <= X2 : x >= X1; x += IET ? 1 : -1) {
                Pixel P = Get(x, y);
                if (!P.Updated) { continue; } // Only attempt reactions on Pixels that have been updated this tick

                Material M = Materials.Index[P.ID];
                Vector2i Pos = new Vector2i(x, y);
                Vector2i Dir = Direction.Random(Direction.Cardinal);

                // Attempt reactions in a random direction
                if (CanReact(M, Pos, Dir)) {
                    break;
                }
            }
        }
    }

    // Draw everything visible in the World
    public unsafe void Draw() {
        // All Pixels
        ImageClearBackground(ref _buffer, Global.BackgroundColor);

        for (int y = Size.Y - 1; y >= 0; y--) {
            for (int x = 0; x < Size.X; x++) {
                Pixel P = Get(x, y);
                if (P.ID != "air") {
                    Color Col = P.Color;
                    if (P.ID == "fire" || P.Burning) { Col = Global.FireColors[RNG.Range(0, 2)]; }
                    ImageDrawPixel(ref _buffer, x, y, Col);
                }
            }
        }

        // Draw Pixels to the render texture
        UpdateTexture(_texture, _buffer.Data);
        DrawTexturePro(_texture, _sourceRect, _destRect, new Vector2(0, 0), 0, Color.White);

        // All Particles (NOTE: Should these be drawn to another render texture?)
        for (int i = Particles.Count - 1; i >= 0; i--) {
            Particle P = Particles[i];
            DrawRectangle((int)P.Position.X * Global.PixelScale, (int)P.Position.Y * Global.PixelScale, Global.PixelScale, Global.PixelScale, P.Color);
        }

        // Debug Drawing
        if (Config.DebugMode) {
            if (Config.DrawChunkBorders) {
                foreach (var C in Chunks) {
                    Color TextCol = C.Awake ? Color.White : Color.DarkGray;
                    DrawRectangleLines(C.Position.X * Global.PixelScale - 1, C.Position.Y * Global.PixelScale - 1, Global.ChunkSize * Global.PixelScale + 1, Global.ChunkSize * Global.PixelScale + 1, Color.DarkGray);
                    Canvas.DrawText($"{C.Position.X / Global.ChunkSize}, {C.Position.Y / Global.ChunkSize} - {C.SleepTimer}", C.Position.X * Global.PixelScale + 5, C.Position.Y * Global.PixelScale + 5, 8, color: TextCol);
                }
            }

            if (Config.DrawUpdateRects) {
                foreach (Chunk C in Chunks) {
                    if (C.Awake) {
                        if (C.X2 == 0 && C.Y2 == 0) {
                            continue;
                        }

                        Rectangle Rect = new Rectangle((C.Position.X + C.X1) * Global.PixelScale, (C.Position.Y + C.Y1) * Global.PixelScale, (C.X2 - C.X1 + 1) * Global.PixelScale, (C.Y2 - C.Y1 + 1) * Global.PixelScale);
                        DrawRectangleLines((int)Rect.X, (int)Rect.Y, (int)Rect.Width, (int)Rect.Height, Color.Green);
                    }
                }
            }
        }
    }
}
