using Riptide;
using Riptide.Transports;
using DisconnectedEventArgs = Riptide.Transports.DisconnectedEventArgs;

namespace NanoPackets.LoopTransport;

/// <summary>
/// Shared plumbing for <see cref="LoopbackClient"/> and <see cref="LoopbackServer"/>: an in-process,
/// queue-based transport with no sockets and no threads. A received message is enqueued rather than
/// raised immediately, and only surfaces once <see cref="Poll"/> is called, so tests control time
/// deterministically by pumping both ends instead of sleeping.
/// </summary>
/// <remarks>
/// <see cref="Disconnected"/> is never raised here - Riptide only expects a transport to raise it for
/// a disconnect the transport itself detects unprompted (a dropped socket, a timeout); every disconnect
/// this transport can produce (<see cref="LoopbackClient.Disconnect"/>, <see cref="LoopbackServer.Close"/>,
/// <see cref="LoopbackServer.Shutdown"/>) is already preceded by Riptide sending a proper application
/// disconnect message down the normal data channel (confirmed against Riptide 2.2.1's own source), which
/// the remote side's Client/Server.Handle already turns into the correctly-reasoned disconnect. Also
/// raising it from here race the two: this transport can service both ends inside one Poll() call
/// (a real transport never could, since the two ends run in different processes), so a locally
/// synthesized transport-level Disconnected would reach Client.LocalDisconnect before the queued data
/// message does, marking the connection disconnected early and discarding the real reason and message.
/// </remarks>
public abstract class LoopbackPeer : IPeer {
    public event EventHandler<DataReceivedEventArgs>? DataReceived;
#pragma warning disable CS0067 // see remarks above: Disconnected is never raised by this transport
    public event EventHandler<DisconnectedEventArgs>? Disconnected;
#pragma warning restore CS0067

    private readonly Queue<Action> pendingEvents = new();

    /// <summary>Drains the events accumulated since the last call and raises them in order.</summary>
    /// <remarks>
    /// Only events queued before this call are drained (the count is snapshotted up front), so an
    /// event handler that itself sends a message - which enqueues onto the *remote* peer's queue -
    /// can never make this loop run longer than one pass over what was already pending here.
    /// </remarks>
    public void Poll() {
        var count = pendingEvents.Count;
        for(var i = 0; i < count; i++) {
            pendingEvents.Dequeue().Invoke();
        }
    }

    internal void Enqueue(Action action) => pendingEvents.Enqueue(action);

    /// <summary>Copies the message (the sender may reuse its buffer) and queues <see cref="DataReceived"/>.</summary>
    internal void HandleMessage(byte[] data, int size, LoopbackConnection fromConnection) {
        var copy = new byte[size];
        Array.Copy(data, copy, size);
        Enqueue(() => DataReceived?.Invoke(this, new DataReceivedEventArgs(copy, size, fromConnection)));
    }
}
