using NanoPackets.Tests.Fixtures;

namespace NanoPackets.Tests.Packets;

[Packet(false, true)]
public readonly ref partial struct ArraysPacket : IServerbound<TestServer> {
    public readonly bool[] BoolArray;
    public readonly byte[] ByteArray;
    public readonly sbyte[] SByteArray;
    public readonly short[] ShortArray;
    public readonly ushort[] UShortArray;
    public readonly int[] IntArray;
    public readonly uint[] UIntArray;
    public readonly long[] LongArray;
    public readonly ulong[] ULongArray;
    public readonly float[] FloatArray;
    public readonly double[] DoubleArray;
    public readonly string[] StringArray;

    public void Serverbound(TestServer network, ushort player) {
        network.Received.BoolArray = BoolArray;
        network.Received.ByteArray = ByteArray;
        network.Received.SByteArray = SByteArray;
        network.Received.ShortArray = ShortArray;
        network.Received.UShortArray = UShortArray;
        network.Received.IntArray = IntArray;
        network.Received.UIntArray = UIntArray;
        network.Received.LongArray = LongArray;
        network.Received.ULongArray = ULongArray;
        network.Received.FloatArray = FloatArray;
        network.Received.DoubleArray = DoubleArray;
        network.Received.StringArray = StringArray;
    }
}
