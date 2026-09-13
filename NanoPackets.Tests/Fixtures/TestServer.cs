using NanoPackets.Packets;
using Riptide.Transports;

namespace NanoPackets.Tests.Fixtures;

public partial class TestServer : NetworkServerBase<TestWorld, TestPlayer, TestPlayer, TestNetPlayer> {
    public ReceivedValues Received { get; } = new();

    /// <summary>Outstanding reliable (Notify-mode) sends, across all clients, still awaiting an ack or a retry.</summary>
    public int PendingReliableMessageCount => reliableMessages.Values.Sum(perClient => perClient.Count);

    public TestServer(TestWorld world, IServer transport, ushort port, ushort maxClientCount = 10)
        : base(world, transport, port, maxClientCount) { }

    protected override TestNetPlayer NewPlayer(ushort id) {
        var player = new TestNetPlayer();
        Players.Add(id, player);
        return player;
    }

    public override void Disconnect(ushort id, string reason = "Disconnected") =>
        Server.DisconnectClient(id, new DisconnectPacket(reason).Write());
}
