using Calcium;

namespace Myriad;

class Chunk {
    public Vector2i Position { get; private set; }
    public int ThreadOrder { get; private set; }

    public bool Awake { get; private set; }
    public bool WakeNextStep { get; private set; }

    public int DX1 { get; private set; }
    public int DY1 { get; private set; }
    public int DX2 { get; private set; }
    public int DY2 { get; private set; }

    private int _x1 = 0;
    private int _y1 = 0;
    private int _x2 = 0;
    private int _y2 = 0;

    private readonly int _rectBuffer = 1;

    public Chunk(Vector2i position, int thread_order) {
        Position = position;
        ThreadOrder = thread_order;
        Awake = true;
    }

    public void Wake(Vector2i pos) {
        WakeNextStep = true;

        if (Awake) {
            var X = pos.X - Position.X;
            var Y = pos.Y - Position.Y;

            _x1 = Math.Clamp(Math.Min(X - _rectBuffer, _x1), 0, Global.ChunkSize);
            _y1 = Math.Clamp(Math.Min(Y - _rectBuffer, _y1), 0, Global.ChunkSize);
            _x2 = Math.Clamp(Math.Max(X + _rectBuffer, _x2), 0, Global.ChunkSize - 1);
            _y2 = Math.Clamp(Math.Max(Y + _rectBuffer, _y2), 0, Global.ChunkSize - 1);
        } else {
            _x1 = 0;
            _x1 = 0;
            _x2 = Global.ChunkSize - 1;
            _y2 = Global.ChunkSize - 1;
        }
    }

    public void Step() {
        UpdateRect();

        Awake = WakeNextStep;
        WakeNextStep = false;
    }

    private void UpdateRect() {
        DX1 = _x1;
        DY1 = _y1;
        DX2 = _x2; 
        DY2 = _y2;
        _x1 = Global.ChunkSize - 1;
        _y1 = Global.ChunkSize - 1;
        _x2 = 0;
        _y2 = 0;
    }
}