namespace NanoPackets.Generator;
public static class Extensions {
    public static string ToCamelCase(this string self) => char.ToLower(self[0]) + self[1..];
}
