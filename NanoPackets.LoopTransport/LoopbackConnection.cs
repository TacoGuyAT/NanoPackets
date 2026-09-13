using Riptide;

namespace NanoPackets.LoopTransport;
public class LoopbackConnection : Connection {
    protected override void Send(byte[] dataBuffer, int amount) {
        throw new NotImplementedException();
    }
}