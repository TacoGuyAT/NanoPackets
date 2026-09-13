using Riptide;

namespace NanoPackets.LoopTransport;

/// <summary>One end of an in-process client/server connection pair.</summary>
public class LoopbackConnection : Connection {
    /// <summary>The peer (client or server) that owns this end of the connection.</summary>
    internal LoopbackPeer OwnerPeer = null!;
    /// <summary>The peer at the other end, which <see cref="Send"/> delivers into.</summary>
    internal LoopbackPeer RemotePeer = null!;
    /// <summary>This connection's counterpart as seen by <see cref="RemotePeer"/>.</summary>
    internal LoopbackConnection RemoteConnection = null!;

    /// <inheritdoc/>
    protected override void Send(byte[] dataBuffer, int amount) => RemotePeer.HandleMessage(dataBuffer, amount, RemoteConnection);
}
