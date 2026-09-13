using Riptide;
using Riptide.Transports;
using Steamworks;
using NetConnection = Steamworks.Data.Connection;
using ConnectionInfo = Steamworks.Data.ConnectionInfo;
using NetIdentity = Steamworks.Data.NetIdentity;

namespace NanoPackets.SteamTransport;

/// <summary>A server which listens for and accepts incoming Steam connections via the Steam Datagram Relay.</summary>
/// <remarks><see cref="Steamworks.SteamClient"/> must be initialized (via <c>SteamClient.Init</c>) before the server is started.</remarks>
public class SteamServer : SteamPeer, IServer, ISocketManager {
    /// <inheritdoc/>
    public event EventHandler<ConnectedEventArgs>? Connected;

    /// <summary>The virtual port the server is listening on.</summary>
    public ushort Port { get; private set; }

    private SocketManager? socketManager;
    private readonly Dictionary<uint, SteamConnection> connections = new();

    /// <summary>Starts the server, listening for connections on the given virtual port.</summary>
    /// <param name="port">The virtual port to listen on. Clients must connect to this same virtual port.</param>
    public void Start(ushort port) {
        if (!Steamworks.SteamClient.IsValid)
            throw new InvalidOperationException("SteamClient must be initialized (SteamClient.Init) before starting the Steam server.");

        Shutdown();
        Port = port;
        socketManager = SteamNetworkingSockets.CreateRelaySocket<SocketManager>(port);
        socketManager.Interface = this;
    }

    /// <inheritdoc/>
    public override void Poll() {
        if (socketManager is null)
            return;

        Steamworks.SteamClient.RunCallbacks();
        socketManager.Receive(MaxMessagesPerPoll);
    }

    /// <inheritdoc/>
    public void Close(Connection connection) {
        if (connection is SteamConnection steamConnection && connections.Remove(steamConnection.SteamNetConnection.Id))
            steamConnection.Close();
    }

    /// <inheritdoc/>
    public void Shutdown() {
        foreach (SteamConnection connection in connections.Values)
            connection.Close();
        connections.Clear();

        socketManager?.Close();
        socketManager = null;
    }

    #region ISocketManager
    /// <inheritdoc/>
    void ISocketManager.OnConnecting(NetConnection connection, ConnectionInfo info) {
        // The connection is accepted automatically by the base SocketManager; nothing to do here.
    }

    /// <inheritdoc/>
    void ISocketManager.OnConnected(NetConnection connection, ConnectionInfo info) {
        if (connections.ContainsKey(connection.Id))
            return;

        SteamConnection steamConnection = new(info.Identity.SteamId, connection);
        connections.Add(connection.Id, steamConnection);
        Connected?.Invoke(this, new ConnectedEventArgs(steamConnection));
    }

    /// <inheritdoc/>
    void ISocketManager.OnDisconnected(NetConnection connection, ConnectionInfo info) {
        if (connections.Remove(connection.Id, out SteamConnection? steamConnection))
            RaiseDisconnected(steamConnection, GetDisconnectReason(info.EndReason));
    }

    /// <inheritdoc/>
    void ISocketManager.OnMessage(NetConnection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel) {
        if (connections.TryGetValue(connection.Id, out SteamConnection? steamConnection))
            HandleMessage(data, size, steamConnection);
    }
    #endregion
}
