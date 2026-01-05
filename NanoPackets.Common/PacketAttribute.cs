namespace NanoPackets;
public class PacketAttribute : Attribute {
    public bool? Ordered;
    public bool? Reliable;

    public PacketAttribute() { }

    public PacketAttribute(bool ordered, bool reliable) {
        Ordered = ordered;
        Reliable = reliable;
    }
}
