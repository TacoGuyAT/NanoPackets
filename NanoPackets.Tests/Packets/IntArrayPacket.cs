using NanoPackets.Tests.Fixtures;

namespace NanoPackets.Tests.Packets;

[Packet(false, true)]
public readonly ref partial struct IntArrayPacket : IServerbound<TestServer> {
    public readonly int[] Values;

    public void Serverbound(TestServer network, ushort player) {
        network.LastIntArray = Values;
    }
}
