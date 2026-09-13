using NanoPackets.Tests.Fixtures;

namespace NanoPackets.Tests.Packets;

[Packet(false, true)]
public readonly ref partial struct PingPongPacket : IClientbound<TestClient>, IServerbound<TestServer> {
    public readonly int Value;

    public void Clientbound(TestClient network, int player) => network.ReceivedFromServer = Value;
    public void Serverbound(TestServer network, ushort player) => network.Received.PingPongValue = Value;
}
