using NanoPackets.Tests.Fixtures;

namespace NanoPackets.Tests.Packets;

[Packet(false, false)]
public readonly ref partial struct UnreliableIntPacket : IServerbound<TestServer> {
    public readonly int Value;
    public void Serverbound(TestServer network, ushort player) => network.Received.IntValue = Value;
}

[Packet(true, true)]
public readonly ref partial struct OrderedIntPacket : IServerbound<TestServer>, IClientbound<TestClient> {
    public readonly int Value;
    public void Serverbound(TestServer network, ushort player) => network.Received.IntValue = Value;
    public void Clientbound(TestClient network, int player) => network.ReceivedFromServer = Value;
}

[Packet]
public readonly ref partial struct DynamicIntPacket : IServerbound<TestServer> {
    public readonly int Value;
    public void Serverbound(TestServer network, ushort player) => network.Received.IntValue = Value;
}
