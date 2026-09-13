namespace NanoPackets.Tests.Fixtures;

/// <summary>Captures whatever the last packet handler(s) received, for test assertions.</summary>
public class ReceivedValues {
    public int? IntValue;
    public ushort? IntFromPlayer;
    public int[]? IntArray;

    public bool BoolValue;
    public byte ByteValue;
    public sbyte SByteValue;
    public short ShortValue;
    public ushort UShortValue;
    public uint UIntValue;
    public long LongValue;
    public ulong ULongValue;
    public float FloatValue;
    public double DoubleValue;
    public string? StringValue;

    public bool[]? BoolArray;
    public byte[]? ByteArray;
    public sbyte[]? SByteArray;
    public short[]? ShortArray;
    public ushort[]? UShortArray;
    public uint[]? UIntArray;
    public long[]? LongArray;
    public ulong[]? ULongArray;
    public float[]? FloatArray;
    public double[]? DoubleArray;
    public string[]? StringArray;

    public (int First, int Second, int Third)? OrderedFields;

    public int[]? EmptyIntArray;
    public string? EmptyString;

    public bool OptionalStringHandlerRan;
    public string? OptionalString;

    public int? PingPongValue;
}
