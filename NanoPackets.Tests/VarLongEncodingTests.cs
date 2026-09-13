using NanoPackets.Tests.Fixtures;
using Riptide;
using Xunit;

namespace NanoPackets.Tests;

[Packet(false, true)]
public readonly ref partial struct SingleLongPacket : IServerbound<TestServer> {
    public readonly long Value;

    public void Serverbound(TestServer network, ushort player) => network.Received.LongValue = Value;
}

[Packet(false, true)]
public readonly ref partial struct SingleULongPacket : IServerbound<TestServer> {
    public readonly ulong Value;

    public void Serverbound(TestServer network, ushort player) => network.Received.ULongValue = Value;
}

/// <summary>
/// Riptide 2.2.1's own <c>Message.AddVarLong(long)</c>/<c>GetVarLong()</c> corrupt any value whose
/// zigzag encoding needs the 64th bit (long.MinValue round-trips as 0, long.MaxValue as -1 - confirmed
/// with no NanoPackets code involved at all), while the unsigned varint writer underneath it is sound
/// across its full range. NanoPackets.Utils.Extensions.AddVarLong/GetVarLong therefore zigzag-encode
/// onto AddVarULong/GetVarULong directly rather than calling Riptide's signed methods; these tests
/// pin that workaround.
/// </summary>
public class VarLongEncodingTests {
    [Theory]
    [InlineData(0uL)]
    [InlineData(ulong.MaxValue)]
    [InlineData(18000000000000000000uL)]
    [InlineData(9223372036854775808uL)] // 2^63 - the first value needing the 10th varint group
    [InlineData(9223372036854775807uL)] // 2^63 - 1 - the last value that fits in 9 groups
    public void UnderlyingUnsignedVarintWriterIsCorrectAcrossItsFullRange(ulong value) {
        var msg = Message.Create();
        msg.AddVarULong(value);
        Assert.Equal(value, msg.GetVarULong());
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    [InlineData(-9000000000000000000L)]
    public void SingleLongFieldRoundTripsAcrossTheFullRange(long value) {
        using var fixture = new NetworkFixture();
        fixture.Pump();

        fixture.Client.Send(new SingleLongPacket(value).Write());
        fixture.Pump();

        Assert.Equal(value, fixture.Server.Received.LongValue);
    }

    [Theory]
    [InlineData(0uL)]
    [InlineData(ulong.MaxValue)]
    [InlineData(18000000000000000000uL)]
    public void SingleULongFieldRoundTripsAcrossTheFullRange(ulong value) {
        using var fixture = new NetworkFixture();
        fixture.Pump();

        fixture.Client.Send(new SingleULongPacket(value).Write());
        fixture.Pump();

        Assert.Equal(value, fixture.Server.Received.ULongValue);
    }
}
