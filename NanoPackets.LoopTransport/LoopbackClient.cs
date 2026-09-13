using Riptide;
using Riptide.Transports;

namespace NanoPackets.LoopTransport;

/// <summary>An in-process client that connects to a <see cref="LoopbackServer"/> listening in the same process.</summary>
public class LoopbackClient : LoopbackPeer, IClient {
    public event EventHandler? Connected;
    public event EventHandler? ConnectionFailed;

    private LoopbackConnection? connection;

    /// <param name="hostAddress">The port a <see cref="LoopbackServer"/> was started on, as a string (e.g. <c>"7777"</c>).</param>
    public bool Connect(string hostAddress, out Connection connection, out string connectError) {
        connection = null!;

        if(!ushort.TryParse(hostAddress, out var port) || !LoopbackServer.TryGet(port, out var server)) {
            connectError = $"No loopback server is listening on port '{hostAddress}'.";
            return false;
        }

        this.connection = server.Accept(this);
        connection = this.connection;
        connectError = string.Empty;

        Enqueue(() => Connected?.Invoke(this, EventArgs.Empty));
        return true;
    }

    public void Disconnect() {
        connection?.RequestDisconnect(DisconnectReason.Disconnected);
        connection = null;
    }
}
