namespace NanoPackets;
public interface IClientbound<T> {
    public void Clientbound(T network, int player);
}
