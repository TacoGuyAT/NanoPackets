namespace NanoPackets;
public interface INetPlayer {
    public int Id { get; }
    public abstract void NetHandleConnect(int id);
    public abstract void NetHandleDisconnect();
}