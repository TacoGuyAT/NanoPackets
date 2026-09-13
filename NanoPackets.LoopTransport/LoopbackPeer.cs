using Riptide;
using Riptide.Transports;
using DisconnectedEventArgs = Riptide.Transports.DisconnectedEventArgs;

namespace NanoPackets.LoopTransport;

/// <summary>
/// Shared plumbing for <see cref="LoopbackClient"/> and <see cref="LoopbackServer"/>: an in-process,
/// queue-based transport with no sockets and no threads. Every event (a received message, a
/// connection completing, a disconnect) is enqueued rather than raised immediately, and only surfaces
/// once <see cref="Poll"/> is called, so tests control time deterministically by pumping both ends
/// instead of sleeping.
/// </summary>
public abstract class LoopbackPeer : IPeer {
    public event EventHandler<DataReceivedEventArgs>? DataReceived;
    public event EventHandler<DisconnectedEventArgs>? Disconnected;

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

    internal void QueueDisconnect(LoopbackConnection connection, DisconnectReason reason) =>
        Enqueue(() => Disconnected?.Invoke(this, new DisconnectedEventArgs(connection, reason)));
}
