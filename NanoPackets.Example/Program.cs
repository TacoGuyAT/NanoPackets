using Riptide;
using Riptide.Transports;
using NanoPackets.Packets;

namespace NanoPackets.Example;

internal class Program {
    static void Main(string[] args) {
        Console.WriteLine("Hello, World!");
        new ClientboundPacket(true, true, 0, Message.Create());
    }
}

public partial class TestServer : NetworkServerBase<World, Player, Player, NetPlayer> {
    public TestServer(World world, IServer transport, ushort port) : base(world, transport, port) { }

    public override void Disconnect(ushort id, string reason = "Disconnected") => throw new NotImplementedException();
    protected override NetPlayer NewPlayer(ushort id) => throw new NotImplementedException();
}

public partial class TestClient : NetworkClientBase<World, Player, Player, NetPlayer> {
    public TestClient(World world, IClient transport, string addr) : base(world, transport, addr) { }

    protected override void OnDisconnect(DisconnectCode code, string reason) { }
}

public class World : IWorld<Player> {
    public Player Player => throw new NotImplementedException();
}

public class Player { }

public class NetPlayer : Player, INetPlayer {
    public int Id => throw new NotImplementedException();

    public void NetHandleConnect(int id) => throw new NotImplementedException();
    public void NetHandleDisconnect() => throw new NotImplementedException();
}