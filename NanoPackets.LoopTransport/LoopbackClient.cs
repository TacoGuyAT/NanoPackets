using Riptide;
using Riptide.Transports;

namespace NanoPackets.LoopTransport;

/// <summary>An in-process client that connects to a <see cref="LoopbackServer"/> listening in the same process.</summary>
public class LoopbackClient : LoopbackPeer, IClient {
    public event EventHandler? Connected;
#pragma warning disable CS0067 // Connect() below never fails - there's no real handshake to reject
    public event EventHandler? ConnectionFailed;
#pragma warning restore CS0067

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

    /// <summary>
    /// Tears down this side of the connection. Purely local: Riptide's own Client.Disconnect() already
    /// sends a MessageHeader.Disconnect message down the normal data channel before calling this, and
    /// that is what informs the server (see the remarks on <see cref="LoopbackPeer"/>).
    /// </summary>
    public void Disconnect() {
        connection = null;
    }
}
