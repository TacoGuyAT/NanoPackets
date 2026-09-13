using Riptide.Transports;

namespace NanoPackets.Tests.Fixtures;

public partial class TestClient : NetworkClientBase<TestWorld, TestPlayer, TestPlayer, TestNetPlayer> {
    public DisconnectCode? LastDisconnectCode { get; private set; }
    public string? LastDisconnectReason { get; private set; }
    public int? ReceivedFromServer { get; set; }

    /// <summary>Outstanding reliable (Notify-mode) sends still awaiting an ack or a retry.</summary>
    public int PendingReliableMessageCount => reliableMessages.Count;

    public TestClient(TestWorld world, IClient transport, string addr) : base(world, transport, addr) { }

    protected override void OnDisconnect(DisconnectCode code, string reason) {
        LastDisconnectCode = code;
        LastDisconnectReason = reason;
    }
}
