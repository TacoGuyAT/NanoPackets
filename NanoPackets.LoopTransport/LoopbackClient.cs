using Riptide;
using Riptide.Transports;

namespace NanoPackets.LoopTransport;
public class LoopbackClient : LoopbackPeer, IClient {
    public event EventHandler Connected;
    public event EventHandler ConnectionFailed;
    public event EventHandler<DataReceivedEventArgs> DataReceived;
    public event EventHandler<Riptide.Transports.DisconnectedEventArgs> Disconnected;

    public bool Connect(string hostAddress, out Connection connection, out string connectError) {
        throw new NotImplementedException();
    }

    public void Disconnect() {
        throw new NotImplementedException();
    }

    public void Poll() {
        throw new NotImplementedException();
    }
}
