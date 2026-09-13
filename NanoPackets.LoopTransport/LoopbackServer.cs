using Riptide;
using Riptide.Transports;

namespace NanoPackets.LoopTransport;

/// <summary>An in-process server that <see cref="LoopbackClient"/>s in the same process can connect to.</summary>
public class LoopbackServer : LoopbackPeer, IServer {
    private static readonly Dictionary<ushort, LoopbackServer> registry = new();

    public event EventHandler<ConnectedEventArgs>? Connected;

    public ushort Port { get; private set; }

    private readonly List<LoopbackConnection> connections = new();

    public void Start(ushort port) {
        Shutdown();
        Port = port;
        registry[port] = this;
    }

    /// <summary>
    /// Tears down this side of the connection. Purely local: Riptide's own Server.DisconnectClient()/
    /// Stop() already send a MessageHeader.Disconnect message down the normal data channel before
    /// calling this, and that is what informs the client (see the remarks on <see cref="LoopbackPeer"/>).
    /// </summary>
    public void Close(Connection connection) {
        if(connection is LoopbackConnection loopbackConnection) {
            connections.Remove(loopbackConnection);
        }
    }

    public void Shutdown() {
        connections.Clear();

        if(registry.TryGetValue(Port, out var self) && self == this) {
            registry.Remove(Port);
        }
    }

    internal static bool TryGet(ushort port, out LoopbackServer server) =>
        registry.TryGetValue(port, out server!);

    /// <summary>Creates the connection pair for an incoming client and returns its end of it.</summary>
    internal LoopbackConnection Accept(LoopbackClient client) {
        var serverSide = new LoopbackConnection { OwnerPeer = this, RemotePeer = client };
        var clientSide = new LoopbackConnection { OwnerPeer = client, RemotePeer = this, RemoteConnection = serverSide };
        serverSide.RemoteConnection = clientSide;

        connections.Add(serverSide);
        Enqueue(() => Connected?.Invoke(this, new ConnectedEventArgs(serverSide)));

        return clientSide;
    }
}
