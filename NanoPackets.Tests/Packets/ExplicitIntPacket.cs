using NanoPackets.Common;
using NanoPackets.Tests.Fixtures;

namespace NanoPackets.Tests.Packets;

[Packet(false, true)]
public readonly ref partial struct ExplicitIntPacket : IServerbound<TestServer> {
    [TransferExplicit]
    public readonly int Value;

    public void Serverbound(TestServer network, ushort player) {
        network.Received.IntValue = Value;
    }
}
