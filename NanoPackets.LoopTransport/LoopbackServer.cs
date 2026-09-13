using Riptide;
using Riptide.Transports;

namespace NanoPackets.LoopTransport;
public class LoopbackServer : LoopbackPeer, IServer {
    public ushort Port { get; private set; }

    public event EventHandler<ConnectedEventArgs> Connected;
    public event EventHandler<DataReceivedEventArgs> DataReceived;
    public event EventHandler<Riptide.Transports.DisconnectedEventArgs> Disconnected;

    public void Start(ushort port) {
        throw new NotImplementedException();
    }

    public void Close(Connection connection) {
        throw new NotImplementedException();
    }

    public void Poll() {
        throw new NotImplementedException();
    }

    public void Shutdown() {
        throw new NotImplementedException();
    }
}