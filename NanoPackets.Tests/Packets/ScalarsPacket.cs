using NanoPackets.Tests.Fixtures;

namespace NanoPackets.Tests.Packets;

[Packet(false, true)]
public readonly ref partial struct ScalarsPacket : IServerbound<TestServer> {
    public readonly bool BoolField;
    public readonly byte ByteField;
    public readonly sbyte SByteField;
    public readonly short ShortField;
    public readonly ushort UShortField;
    public readonly int IntField;
    public readonly uint UIntField;
    public readonly long LongField;
    public readonly ulong ULongField;
    public readonly float FloatField;
    public readonly double DoubleField;
    public readonly string StringField;

    public void Serverbound(TestServer network, ushort player) {
        network.Received.BoolValue = BoolField;
        network.Received.ByteValue = ByteField;
        network.Received.SByteValue = SByteField;
        network.Received.ShortValue = ShortField;
        network.Received.UShortValue = UShortField;
        network.Received.IntValue = IntField;
        network.Received.UIntValue = UIntField;
        network.Received.LongValue = LongField;
        network.Received.ULongValue = ULongField;
        network.Received.FloatValue = FloatField;
        network.Received.DoubleValue = DoubleField;
        network.Received.StringValue = StringField;
    }
}
