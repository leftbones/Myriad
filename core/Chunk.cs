using Calcium;

namespace Myriad.Core;

internal class Chunk {
    public Vector2i Position { get; private set; }
    public int ThreadOrder { get; private set; }

    public bool Awake { get; private set; }
    public bool WakeNextStep { get; private set; }

    public int X1 { get; private set; }
    public int Y1 { get; private set; }
    public int X2 { get; private set; }
    public int Y2 { get; private set; }

    private int _x1 = 0;
    private int _y1 = 0;
    private int _x2 = 0;
    private int _y2 = 0;

    private readonly int _rectBuffer = 2;

    private readonly int _timeToSleep = 60;

    public int SleepTimer { get; private set; }

    public Chunk(Vector2i position, int thread_order) {
        Position = position;
        ThreadOrder = thread_order;
        Awake = false;
    }

    public void Wake(Vector2i pos) {
        WakeNextStep = true;
        SleepTimer = _timeToSleep;

        if (Awake) {
            int X = pos.X - Position.X;
            int Y = pos.Y - Position.Y;

            _x1 = Math.Clamp(Math.Min(X - _rectBuffer, _x1), 0, Global.ChunkSize);
            _y1 = Math.Clamp(Math.Min(Y - _rectBuffer, _y1), 0, Global.ChunkSize);
            _x2 = Math.Clamp(Math.Max(X + _rectBuffer, _x2), 0, Global.ChunkSize - 1);
            _y2 = Math.Clamp(Math.Max(Y + _rectBuffer, _y2), 0, Global.ChunkSize - 1);
        } else {
            _x1 = 0;
            _y1 = 0;
            _x2 = Global.ChunkSize - 1;
            _y2 = Global.ChunkSize - 1;
        }
    }

    public void Step() {
        UpdateRect();

        if (Awake && !WakeNextStep) {
            SleepTimer--;
            if (SleepTimer == 0) {
                Awake = false;
            }
        } else {
            Awake = WakeNextStep;
        }

        WakeNextStep = false;
    }

    private void UpdateRect() {
        X1 = _x1;
        Y1 = _y1;
        X2 = _x2;
        Y2 = _y2;
        _x1 = Global.ChunkSize - 1;
        _y1 = Global.ChunkSize - 1;
        _x2 = 0;
        _y2 = 0;
    }
}
