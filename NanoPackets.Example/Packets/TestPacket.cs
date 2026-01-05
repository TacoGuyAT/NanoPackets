namespace NanoPackets.Example.Packets;

[Packet(false, true)]
public readonly ref partial struct TestPacket : IClientbound<TestClient>, IServerbound<TestServer> {
    public const int CONST_FIELD = 1;
    public readonly int[] ArrayField;
    public readonly string TextField;

    public void Clientbound(TestClient network, int player) { }
    public void Serverbound(TestServer network, ushort player) { }
}
    