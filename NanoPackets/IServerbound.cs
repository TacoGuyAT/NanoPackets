namespace NanoPackets;
public interface IServerbound<T> {
    public void Serverbound(T network, ushort player);
}
