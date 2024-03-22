using Calcium;

namespace Myriad;

class Chunk {
    public Vector2i Position { get; private set; }
    public Vector2i Size { get; private set; }

    public Chunk(Vector2i position, Vector2i size) {
        Position = position;
        Size = size;
    }
}