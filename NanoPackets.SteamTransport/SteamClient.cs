using Riptide;
using Riptide.Transports;
using Steamworks;
using ConnectionInfo = Steamworks.Data.ConnectionInfo;

namespace NanoPackets.SteamTransport;

/// <summary>A client which connects to a <see cref="SteamServer"/> via the Steam Datagram Relay.</summary>
/// <remarks><see cref="Steamworks.SteamClient"/> must be initialized (via <c>SteamClient.Init</c>) before connecting.</remarks>
public class SteamClient : SteamPeer, IClient, IConnectionManager {
    /// <inheritdoc/>
    public event EventHandler? Connected;
    /// <inheritdoc/>
    public event EventHandler? ConnectionFailed;

    private ConnectionManager? connectionManager;
    private SteamConnection? steamConnection;
    private bool isConnected;

    /// <summary>Connects to a Steam server.</summary>
    /// <param name="hostAddress">The server's Steam ID, optionally followed by <c>:virtualPort</c> (e.g. <c>"76561198000000000"</c> or <c>"76561198000000000:1"</c>).</param>
    /// <param name="connection">The resulting connection, if the connection attempt could be initiated.</param>
    /// <param name="connectError">A description of why the connection attempt could not be initiated, if applicable.</param>
    /// <returns>Whether the connection attempt was initiated.</returns>
    public bool Connect(string hostAddress, out Connection connection, out string connectError) {
        connection = null!;

        if (!Steamworks.SteamClient.IsValid) {
            connectError = "SteamClient must be initialized (SteamClient.Init) before connecting.";
            return false;
        }

        if (!TryParseHostAddress(hostAddress, out SteamId serverId, out int virtualPort, out connectError))
            return false;

        isConnected = false;
        connectionManager = SteamNetworkingSockets.ConnectRelay<ConnectionManager>(serverId, virtualPort);
        connectionManager.Interface = this;

        steamConnection = new SteamConnection(serverId, connectionManager.Connection);
        connection = steamConnection;
        connectError = string.Empty;
        return true;
    }

    /// <inheritdoc/>
    public void Disconnect() {
        steamConnection?.Close();
        steamConnection = null;
        connectionManager = null;
        isConnected = false;
    }

    /// <inheritdoc/>
    public override void Poll() {
        if (connectionManager is null)
            return;

        Steamworks.SteamClient.RunCallbacks();
        connectionManager.Receive(MaxMessagesPerPoll);
    }

    /// <summary>Parses a host address of the form <c>"steamId"</c> or <c>"steamId:virtualPort"</c>.</summary>
    private static bool TryParseHostAddress(string hostAddress, out SteamId steamId, out int virtualPort, out string error) {
        steamId = default;
        virtualPort = 0;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(hostAddress)) {
            error = "Host address is empty.";
            return false;
        }

        string[] parts = hostAddress.Split(':');
        if (!ulong.TryParse(parts[0], out ulong id)) {
            error = $"'{parts[0]}' is not a valid Steam ID.";
            return false;
        }
        steamId = new SteamId { Value = id };

        if (parts.Length > 1 && !int.TryParse(parts[1], out virtualPort)) {
            error = $"'{parts[1]}' is not a valid virtual port.";
            return false;
        }

        return true;
    }

    #region IConnectionManager
    /// <inheritdoc/>
    void IConnectionManager.OnConnecting(ConnectionInfo info) {
        // The relay connection is being established; nothing to do until it succeeds or fails.
    }

    /// <inheritdoc/>
    void IConnectionManager.OnConnected(ConnectionInfo info) {
        isConnected = true;
        Connected?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    void IConnectionManager.OnDisconnected(ConnectionInfo info) {
        if (isConnected) {
            if (steamConnection is not null)
                RaiseDisconnected(steamConnection, GetDisconnectReason(info.EndReason));
        }
        else {
            // The relay connection failed before it was ever established.
            ConnectionFailed?.Invoke(this, EventArgs.Empty);
        }

        steamConnection = null;
        connectionManager = null;
        isConnected = false;
    }

    /// <inheritdoc/>
    void IConnectionManager.OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel) {
        if (steamConnection is not null)
            HandleMessage(data, size, steamConnection);
    }
    #endregion
}
