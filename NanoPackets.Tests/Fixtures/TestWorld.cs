namespace NanoPackets.Tests.Fixtures;

public class TestWorld : IWorld<TestPlayer> {
    public TestPlayer Player { get; } = new();
}
