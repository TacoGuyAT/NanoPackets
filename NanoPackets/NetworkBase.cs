using Riptide;

namespace NanoPackets;
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