using NanoPackets.Tests.Fixtures;

namespace NanoPackets.Tests.Packets;

[Packet(false, true)]
public readonly ref partial struct OrderedFieldsPacket : IServerbound<TestServer> {
    public readonly int First;
    public readonly int Second;
    public readonly int Third;

    public void Serverbound(TestServer network, ushort player) {
        network.Received.OrderedFields = (First, Second, Third);
    }
}
