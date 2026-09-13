using NanoPackets.Tests.Fixtures;

namespace NanoPackets.Tests.Packets;

[Packet(false, true)]
public readonly ref partial struct IntPacket : IServerbound<TestServer> {
    public readonly int Value;

    public void Serverbound(TestServer network, ushort player) {
        network.LastIntValue = Value;
        network.LastIntFromPlayer = player;
    }
}
