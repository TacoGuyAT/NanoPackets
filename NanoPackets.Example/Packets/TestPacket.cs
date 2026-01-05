namespace NanoPackets.Example.Packets;

[Packet(false, true)]
public readonly ref partial struct TestPacket {
    public const int CONST_FIELD = 1;
    public readonly int[] ArrayField;
    public readonly string TextField;

    void Serverbound(TestServer network, int player) {
    }
    void Clientbound(TestClient network, int player) {
    }
}
    