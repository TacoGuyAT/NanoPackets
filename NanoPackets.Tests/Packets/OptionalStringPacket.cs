using NanoPackets.Tests.Fixtures;

namespace NanoPackets.Tests.Packets;

[Packet(false, true)]
public readonly ref partial struct OptionalStringPacket : IServerbound<TestServer> {
    public readonly string? MaybeText;

    public void Serverbound(TestServer network, ushort player) {
        network.Received.OptionalStringHandlerRan = true;
        network.Received.OptionalString = MaybeText;
    }
}
