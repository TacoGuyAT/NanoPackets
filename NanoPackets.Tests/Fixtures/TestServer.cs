using NanoPackets.Packets;
using Riptide.Transports;

namespace NanoPackets.Tests.Fixtures;

public partial class TestServer : NetworkServerBase<TestWorld, TestPlayer, TestPlayer, TestNetPlayer> {
    public int? LastIntValue { get; set; }
    public ushort? LastIntFromPlayer { get; set; }
    public int[]? LastIntArray { get; set; }

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
