using NanoPackets.LoopTransport;

namespace NanoPackets.Tests.Fixtures;

/// <summary>
/// A server and a client joined by an in-memory <see cref="LoopbackServer"/>/<see cref="LoopbackClient"/>
/// pair. Nothing here uses real time or threads; call <see cref="Pump"/> to advance both sides until
/// their queued work (the connection handshake, sent packets, disconnects) has been delivered.
/// </summary>
public sealed class NetworkFixture : IDisposable {
    private static int portCounter = 40000;
    private readonly ushort port;
    private readonly List<TestClient> extraClients = new();

    public TestWorld ServerWorld { get; } = new();
    public TestWorld ClientWorld { get; } = new();
    public TestServer Server { get; }
    public TestClient Client { get; }

    public NetworkFixture(ushort maxClientCount = 10) {
        port = NextPort();
        Server = new TestServer(ServerWorld, new LoopbackServer(), port, maxClientCount);
        Client = new TestClient(ClientWorld, new LoopbackClient(), port.ToString());
    }

    private static ushort NextPort() => (ushort)Interlocked.Increment(ref portCounter);

    /// <summary>Connects another client to the same server, for tests that need more than one.</summary>
    public TestClient AddClient() {
        var client = new TestClient(new TestWorld(), new LoopbackClient(), port.ToString());
        extraClients.Add(client);
        return client;
    }

    /// <summary>Advances every peer's Riptide pump (which polls the transport in turn) several times over.</summary>
    public void Pump(int rounds = 10) {
        for(var i = 0; i < rounds; i++) {
            Client.Client.Update();
            foreach(var client in extraClients) {
                client.Client.Update();
            }
            Server.Server.Update();
        }
    }

    public void Dispose() {
        Client.Client.Disconnect();
        foreach(var client in extraClients) {
            client.Client.Disconnect();
        }
        Server.Server.Stop();
    }
}
