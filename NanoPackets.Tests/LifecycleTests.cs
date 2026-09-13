using NanoPackets.Tests.Fixtures;
using NanoPackets.Tests.Packets;
using Riptide;
using Xunit;

namespace NanoPackets.Tests;

public class LifecycleTests {
    [Fact]
    public void ReliablePacketUsesReliableSendMode() {
        Assert.Equal(MessageSendMode.Reliable, new IntPacket(1).Write().SendMode);
    }

    [Fact]
    public void UnreliablePacketUsesUnreliableSendMode() {
        Assert.Equal(MessageSendMode.Unreliable, new UnreliableIntPacket(1).Write().SendMode);
    }

    [Fact]
    public void OrderedPacketUsesNotifySendMode() {
        Assert.Equal(MessageSendMode.Notify, new OrderedIntPacket(1).Write().SendMode);
    }

    [Theory]
    [InlineData(true, true, MessageSendMode.Notify)]
    [InlineData(true, false, MessageSendMode.Notify)]
    [InlineData(false, true, MessageSendMode.Reliable)]
    [InlineData(false, false, MessageSendMode.Unreliable)]
    public void DynamicPacketSendModeIsChosenAtRuntime(bool ordered, bool reliable, MessageSendMode expected) {
        Assert.Equal(expected, new DynamicIntPacket(ordered, reliable, 1).Write().SendMode);
    }

    [Fact]
    public void DynamicPacketStillRoundTripsCorrectly() {
        using var fixture = new NetworkFixture();
        fixture.Pump();

        fixture.Client.Send(new DynamicIntPacket(false, true, 123).Write());
        fixture.Pump();

        Assert.Equal(123, fixture.Server.Received.IntValue);
    }

    [Fact]
    public void ClientDisconnectRaisesOnDisconnect() {
        using var fixture = new NetworkFixture();
        fixture.Pump();

        fixture.Client.Client.Disconnect();
        fixture.Pump();

        Assert.NotNull(fixture.Client.LastDisconnectCode);
    }

    [Fact]
    public void ServerSideDisconnectIsObservedByTheClient() {
        using var fixture = new NetworkFixture();
        fixture.Pump();

        fixture.Server.Disconnect(fixture.Client.Client.Id, "kicked");
        fixture.Pump();

        Assert.Equal(DisconnectCode.Generic, fixture.Client.LastDisconnectCode);
        Assert.Equal("kicked", fixture.Client.LastDisconnectReason);
    }

    [Fact]
    public void ClientSideReliableBookkeepingEmptiesAfterDelivery() {
        using var fixture = new NetworkFixture();
        fixture.Pump();

        // Notify-mode acks piggyback on the next notify-mode message sent in the *other* direction
        // (there's no dedicated ack packet), and only once that direction has actually processed the
        // original message - so the reply must be sent only after a pump has delivered it.
        fixture.Client.Send(new OrderedIntPacket(1).Write());
        fixture.Pump();
        fixture.Server.Broadcast(new OrderedIntPacket(2).Write(), senderId: 0);
        fixture.Pump();

        Assert.Equal(0, fixture.Client.PendingReliableMessageCount);
    }

    [Fact]
    public void ServerSideReliableBookkeepingEmptiesAfterDelivery() {
        using var fixture = new NetworkFixture();
        fixture.Pump();

        fixture.Server.Broadcast(new OrderedIntPacket(1).Write(), senderId: 0);
        fixture.Pump();
        fixture.Client.Send(new OrderedIntPacket(2).Write());
        fixture.Pump();

        Assert.Equal(0, fixture.Server.PendingReliableMessageCount);
    }
}
