namespace Myriad;

class Pixel {
    public int ID { get; private set; }
    public bool Updated { get; set; }

    public Pixel(int? id=null) {
        ID = id ?? -1;
    }
}