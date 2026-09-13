using NanoPackets.Tests.Fixtures;
using NanoPackets.Tests.Packets;
using Xunit;

namespace NanoPackets.Tests;

public class SerializationTests {
    [Fact]
    public void EveryScalarTypeRoundTrips() {
        using var fixture = new NetworkFixture();
        fixture.Pump();

        fixture.Client.Send(new ScalarsPacket(
            true, 200, -100, -30000, 60000, -70000, 4000000000,
            -9000000000000000000, 18000000000000000000, 3.14f, 2.71828, "hello").Write());
        fixture.Pump();

        var received = fixture.Server.Received;
        Assert.True(received.BoolValue);
        Assert.Equal(200, received.ByteValue);
        Assert.Equal(-100, received.SByteValue);
        Assert.Equal(-30000, received.ShortValue);
        Assert.Equal(60000, received.UShortValue);
        Assert.Equal(-70000, received.IntValue);
        Assert.Equal(4000000000u, received.UIntValue);
        Assert.Equal(-9000000000000000000L, received.LongValue);
        Assert.Equal(18000000000000000000uL, received.ULongValue);
        Assert.Equal(3.14f, received.FloatValue);
        Assert.Equal(2.71828, received.DoubleValue);
        Assert.Equal("hello", received.StringValue);
    }

    [Fact]
    public void ArraysOfEveryScalarTypeRoundTrip() {
        using var fixture = new NetworkFixture();
        fixture.Pump();

        var bools = new[] { true, false, true };
        var bytes = new byte[] { 0, 128, 255 };
        var sbytes = new sbyte[] { -128, 0, 127 };
        var shorts = new short[] { short.MinValue, 0, short.MaxValue };
        var ushorts = new ushort[] { 0, ushort.MaxValue };
        var ints = new[] { int.MinValue, 0, int.MaxValue };
        var uints = new uint[] { 0, uint.MaxValue };
        var longs = new[] { long.MinValue, 0L, long.MaxValue };
        var ulongs = new ulong[] { 0, ulong.MaxValue };
        var floats = new[] { -1.5f, 0f, 1.5f };
        var doubles = new[] { -1.5, 0, 1.5 };
        var strings = new[] { "a", "b", "c" };

        fixture.Client.Send(new ArraysPacket(
            bools, bytes, sbytes, shorts, ushorts, ints, uints, longs, ulongs, floats, doubles, strings).Write());
        fixture.Pump();

        var received = fixture.Server.Received;
        Assert.Equal(bools, received.BoolArray);
        Assert.Equal(bytes, received.ByteArray);
        Assert.Equal(sbytes, received.SByteArray);
        Assert.Equal(shorts, received.ShortArray);
        Assert.Equal(ushorts, received.UShortArray);
        Assert.Equal(ints, received.IntArray);
        Assert.Equal(uints, received.UIntArray);
        Assert.Equal(longs, received.LongArray);
        Assert.Equal(ulongs, received.ULongArray);
        Assert.Equal(floats, received.FloatArray);
        Assert.Equal(doubles, received.DoubleArray);
        Assert.Equal(strings, received.StringArray);
    }

    [Fact]
    public void EmptyArrayAndEmptyStringRoundTrip() {
        using var fixture = new NetworkFixture();
        fixture.Pump();

        fixture.Client.Send(new EmptyValuesPacket(Array.Empty<int>(), string.Empty).Write());
        fixture.Pump();

        Assert.Empty(fixture.Server.Received.EmptyIntArray!);
        Assert.Equal(string.Empty, fixture.Server.Received.EmptyString);
    }

    [Theory]
    [InlineData("hello")]
    [InlineData(null)]
    public void OptionalStringRoundTrips(string? value) {
        using var fixture = new NetworkFixture();
        fixture.Pump();

        fixture.Client.Send(new OptionalStringPacket(value).Write());
        fixture.Pump();

        Assert.True(fixture.Server.Received.OptionalStringHandlerRan);
        Assert.Equal(value, fixture.Server.Received.OptionalString);
    }

    [Fact]
    public void FieldOrderIsPreservedAcrossSameTypedFields() {
        using var fixture = new NetworkFixture();
        fixture.Pump();

        fixture.Client.Send(new OrderedFieldsPacket(1, 2, 3).Write());
        fixture.Pump();

        Assert.Equal((1, 2, 3), fixture.Server.Received.OrderedFields);
    }

    [Fact]
    public void TransferExplicitUsesFixedWidthRatherThanVarint() {
        var varintMessage = new IntPacket(5).Write();
        var explicitMessage = new ExplicitIntPacket(5).Write();

        // A small value like 5 fits in a single varint byte; TransferExplicit always writes the full
        // fixed width (4 bytes for int), so the encodings must differ in length for the same value.
        Assert.True(explicitMessage.WrittenBits > varintMessage.WrittenBits);
    }

    [Fact]
    public void TransferExplicitFieldStillRoundTripsCorrectly() {
        using var fixture = new NetworkFixture();
        fixture.Pump();

        fixture.Client.Send(new ExplicitIntPacket(-12345).Write());
        fixture.Pump();

        Assert.Equal(-12345, fixture.Server.Received.IntValue);
    }
}
