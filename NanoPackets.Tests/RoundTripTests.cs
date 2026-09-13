using NanoPackets.Tests.Fixtures;
using NanoPackets.Tests.Packets;
using Xunit;

namespace NanoPackets.Tests;

public class RoundTripTests {
    [Fact]
    public void ServerHandlerReceivesFieldValueSentByClient() {
        using var fixture = new NetworkFixture();
        fixture.Pump();

        fixture.Client.Send(new IntPacket(42).Write());
        fixture.Pump();

        Assert.Equal(42, fixture.Server.LastIntValue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void NegativeAndBoundarySignedValuesRoundTrip(int value) {
        using var fixture = new NetworkFixture();
        fixture.Pump();

        fixture.Client.Send(new IntPacket(value).Write());
        fixture.Pump();

        Assert.Equal(value, fixture.Server.LastIntValue);
    }

    [Fact]
    public void ArrayOfMixedSignValuesRoundTrips() {
        using var fixture = new NetworkFixture();
        fixture.Pump();

        var values = new[] { 0, -1, 1, int.MinValue, int.MaxValue, -42 };
        fixture.Client.Send(new IntArrayPacket(values).Write());
        fixture.Pump();

        Assert.Equal(values, fixture.Server.LastIntArray);
    }
}
