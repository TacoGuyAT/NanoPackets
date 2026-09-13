namespace NanoPackets.Tests.Fixtures;

public class TestPlayer {
    public bool Disconnected { get; private set; }
    public virtual void MarkDisconnected() => Disconnected = true;
}

public class TestNetPlayer : TestPlayer, INetPlayer {
    public int Id { get; private set; } = -1;
    public bool ConnectHandled { get; private set; }

    public void NetHandleConnect(int id) {
        Id = id;
        ConnectHandled = true;
    }

    public void NetHandleDisconnect() => MarkDisconnected();
}
