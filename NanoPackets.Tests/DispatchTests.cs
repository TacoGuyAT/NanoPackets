using NanoPackets.Tests.Fixtures;
using NanoPackets.Tests.Packets;
using Riptide;
using Xunit;

namespace NanoPackets.Tests;

public class DispatchTests {
    [Fact]
    public void TwoDifferentPacketTypesSentInSequenceEachReachTheirOwnHandler() {
        using var fixture = new NetworkFixture();
        fixture.Pump();

        fixture.Client.Send(new IntPacket(11).Write());
        fixture.Client.Send(new OrderedFieldsPacket(1, 2, 3).Write());
        fixture.Pump();

        Assert.Equal(11, fixture.Server.Received.IntValue);
        Assert.Equal((1, 2, 3), fixture.Server.Received.OrderedFields);
    }

    [Fact]
    public void StructImplementingBothDirectionsFiresTheCorrectHandlerPerDirection() {
        using var fixture = new NetworkFixture();
        fixture.Pump();

        fixture.Client.Send(new PingPongPacket(7).Write());
        fixture.Pump();

        Assert.Equal(7, fixture.Server.Received.PingPongValue);
        Assert.Null(fixture.Client.ReceivedFromServer);

        fixture.Server.Broadcast(new PingPongPacket(99).Write(), senderId: 0);
        fixture.Pump();

        Assert.Equal(99, fixture.Client.ReceivedFromServer);
    }

    [Fact]
    public void UnrecognizedPacketIdDisconnectsTheClientWithIncorrectPacketSequence() {
        using var fixture = new NetworkFixture();
        fixture.Pump();

        var garbage = Message.Create(MessageSendMode.Reliable, (ushort)9999);
        fixture.Server.Broadcast(garbage, senderId: 0);
        fixture.Pump();

        Assert.Equal(DisconnectCode.IncorrectPacketSequence, fixture.Client.LastDisconnectCode);
    }

    [Fact]
    public void BroadcastReachesEveryConnectedClient() {
        using var fixture = new NetworkFixture();
        var second = fixture.AddClient();
        fixture.Pump();

        fixture.Server.Broadcast(new PingPongPacket(55).Write(), senderId: 0);
        fixture.Pump();

        Assert.Equal(55, fixture.Client.ReceivedFromServer);
        Assert.Equal(55, second.ReceivedFromServer);
    }

    [Fact]
    public void BroadcastExceptSkipsTheNamedClient() {
        using var fixture = new NetworkFixture();
        var second = fixture.AddClient();
        fixture.Pump();

        var excludedId = fixture.Client.Client.Id;
        fixture.Server.BroadcastExcept(new PingPongPacket(77).Write(), excludedId);
        fixture.Pump();

        Assert.Null(fixture.Client.ReceivedFromServer);
        Assert.Equal(77, second.ReceivedFromServer);
    }
}
