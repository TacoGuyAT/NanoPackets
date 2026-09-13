using Riptide;

namespace NanoPackets;

/// <remarks>
/// Not thread-safe. Riptide raises its connection/message events on whichever thread pumps the
/// underlying peer (i.e. the thread that calls <c>Server.Update()</c>/<c>Client.Update()</c>), and
/// the internal collections here (<see cref="Players"/>, the reliable-message maps in the derived
/// server/client) assume that all such callbacks plus your own <c>Send</c>/<c>QueueToFrame</c> calls
/// happen on a single thread. Pump and use a given instance from one thread only.
/// </remarks>
public abstract class NetworkBase<TWorld, TPlayerBase, TPlayer>
    where TWorld : IWorld<TPlayer>
    where TPlayer : TPlayerBase
{
    //public const ushort PORT = 59159;
    //public const ushort PROTOCOL = 0;
    public TWorld World { get; private set; }
    public bool IsOrderedFrame { get; set; }
    public bool IsReliableFrame { get; set; }
    public List<Message> Frame { get; init; } = new();
    public TPlayer Player => World.Player;
    public Dictionary<int, TPlayerBase> Players;
    /// <summary>
    /// Peer will be:
    /// - Server for host
    /// - Client for connected player
    /// </summary>
    public Peer Peer { get; protected set; }
    public NetworkBase(TWorld world, Peer peer) {
        World = world;
        Players = new();
        Peer = peer;
    }

    public abstract void Send(Message msg);

    public void QueueToFrame(Message msg) {
        IsOrderedFrame |= msg.SendMode == MessageSendMode.Notify;
        IsReliableFrame |= IsOrderedFrame ? msg.GetBool() : msg.SendMode == MessageSendMode.Reliable;

        Frame.Add(msg);
    }
}