using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NanoPackets.Generator;
using Xunit;

namespace NanoPackets.Tests;

/// <summary>
/// Drives MainGenerator directly through the Roslyn APIs against small, isolated snippets, rather than
/// round-tripping packets - the right tool for diagnostics, which fire (or don't) per-compilation
/// regardless of whether anything actually gets sent.
/// </summary>
public class GeneratorDiagnosticsTests {
    private const string Scaffold = """
        using NanoPackets;
        using Riptide;
        using Riptide.Transports;

        public class World : IWorld<Player> { public Player Player => null!; }
        public class Player { }
        public class NetPlayer : Player, INetPlayer {
            public int Id => 0;
            public void NetHandleConnect(int id) { }
            public void NetHandleDisconnect() { }
        }
        """;

    private const string ServerA = """
        public partial class ServerA : NetworkServerBase<World, Player, Player, NetPlayer> {
            public ServerA(World world, IServer transport, ushort port) : base(world, transport, port) { }
            protected override NetPlayer NewPlayer(ushort id) => throw new System.NotImplementedException();
            public override void Disconnect(ushort id, string reason = "Disconnected") => throw new System.NotImplementedException();
        }
        """;

    private const string ServerB = """
        public partial class ServerB : NetworkServerBase<World, Player, Player, NetPlayer> {
            public ServerB(World world, IServer transport, ushort port) : base(world, transport, port) { }
            protected override NetPlayer NewPlayer(ushort id) => throw new System.NotImplementedException();
            public override void Disconnect(ushort id, string reason = "Disconnected") => throw new System.NotImplementedException();
        }
        """;

    private const string ClientA = """
        public partial class ClientA : NetworkClientBase<World, Player, Player, NetPlayer> {
            public ClientA(World world, IClient transport, string addr) : base(world, transport, addr) { }
            protected override void OnDisconnect(DisconnectCode code, string reason) { }
        }
        """;

    private const string ClientB = """
        public partial class ClientB : NetworkClientBase<World, Player, Player, NetPlayer> {
            public ClientB(World world, IClient transport, string addr) : base(world, transport, addr) { }
            protected override void OnDisconnect(DisconnectCode code, string reason) { }
        }
        """;

    private static ImmutableArray<Diagnostic> RunGenerator(params string[] sources) {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(PacketAttribute).Assembly.Location))
            .Append(MetadataReference.CreateFromFile(typeof(NetworkBase<,,>).Assembly.Location))
            .Append(MetadataReference.CreateFromFile(typeof(Riptide.Message).Assembly.Location))
            .ToList();

        // One combined syntax tree rather than one per source: `using` directives are file-scoped, and
        // every fragment below relies on Scaffold's usings (NanoPackets, Riptide, Riptide.Transports).
        var combinedSource = string.Join("\n", sources);
        var compilation = CSharpCompilation.Create(
            "GeneratorDiagnosticsTests.Snippet",
            new[] { CSharpSyntaxTree.ParseText(combinedSource) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(new MainGenerator());
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
        return driver.GetRunResult().Results[0].Diagnostics;
    }

    [Fact]
    public void MultipleServersFireNP0001ButNotNP0002() {
        var diagnostics = RunGenerator(Scaffold, ServerA, ServerB, ClientA);

        Assert.Contains(diagnostics, d => d.Id == "NP0001");
        Assert.DoesNotContain(diagnostics, d => d.Id == "NP0002");
    }

    [Fact]
    public void MultipleClientsFireNP0002ButNotNP0001() {
        var diagnostics = RunGenerator(Scaffold, ServerA, ClientA, ClientB);

        Assert.Contains(diagnostics, d => d.Id == "NP0002");
        Assert.DoesNotContain(diagnostics, d => d.Id == "NP0001");
    }

    [Fact]
    public void MissingServerFiresNP0003ButNotNP0004() {
        var diagnostics = RunGenerator(Scaffold, ClientA);

        Assert.Contains(diagnostics, d => d.Id == "NP0003");
        Assert.DoesNotContain(diagnostics, d => d.Id == "NP0004");
    }

    [Fact]
    public void MissingClientFiresNP0004ButNotNP0003() {
        var diagnostics = RunGenerator(Scaffold, ServerA);

        Assert.Contains(diagnostics, d => d.Id == "NP0004");
        Assert.DoesNotContain(diagnostics, d => d.Id == "NP0003");
    }

    [Fact]
    public void NeitherMissingDiagnosticFiresWhenBothArePresent() {
        var diagnostics = RunGenerator(Scaffold, ServerA, ClientA);

        Assert.DoesNotContain(diagnostics, d => d.Id == "NP0003");
        Assert.DoesNotContain(diagnostics, d => d.Id == "NP0004");
    }

    [Fact]
    public void PacketWithOnlyConstFieldsFiresNP0005() {
        const string packet = """
            using NanoPackets;
            [Packet(false, true)]
            public partial struct ConstOnlyPacket {
                public const int X = 1;
            }
            """;

        var diagnostics = RunGenerator(Scaffold, ServerA, ClientA, packet);

        Assert.Contains(diagnostics, d => d.Id == "NP0005");
    }

    [Fact]
    public void PacketWithFieldsDoesNotFireNP0005() {
        const string packet = """
            using NanoPackets;
            [Packet(false, true)]
            public partial struct FieldedPacket {
                public int Value;
            }
            """;

        var diagnostics = RunGenerator(Scaffold, ServerA, ClientA, packet);

        Assert.DoesNotContain(diagnostics, d => d.Id == "NP0005");
    }

    [Fact]
    public void NonPartialPacketFiresNP0006() {
        const string packet = """
            using NanoPackets;
            [Packet(false, true)]
            public struct NotPartialPacket {
                public int Value;
            }
            """;

        var diagnostics = RunGenerator(Scaffold, ServerA, ClientA, packet);

        Assert.Contains(diagnostics, d => d.Id == "NP0006");
    }

    [Fact]
    public void PartialPacketDoesNotFireNP0006() {
        const string packet = """
            using NanoPackets;
            [Packet(false, true)]
            public partial struct PartialPacket {
                public int Value;
            }
            """;

        var diagnostics = RunGenerator(Scaffold, ServerA, ClientA, packet);

        Assert.DoesNotContain(diagnostics, d => d.Id == "NP0006");
    }
}
