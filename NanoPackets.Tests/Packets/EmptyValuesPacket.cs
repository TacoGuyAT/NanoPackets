using NanoPackets.Tests.Fixtures;

namespace NanoPackets.Tests.Packets;

[Packet(false, true)]
public readonly ref partial struct EmptyValuesPacket : IServerbound<TestServer> {
    public readonly int[] EmptyIntArray;
    public readonly string EmptyString;

    public void Serverbound(TestServer network, ushort player) {
        network.Received.EmptyIntArray = EmptyIntArray;
        network.Received.EmptyString = EmptyString;
    }
}
