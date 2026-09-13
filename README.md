# NanoPackets

A lightweight, code-generated networking layer for .NET and Godot, built on top of
[Riptide](https://github.com/RiptideNetworking/Riptide). You declare packets as plain structs;
a Roslyn source generator writes the serialization, packet-id enum, and handler dispatch for you,
so there is no reflection and no hand-written read/write boilerplate at runtime.

## Projects

| Project | Target | Purpose |
|---|---|---|
| `NanoPackets` | net9.0 | Core runtime: `NetworkServerBase` / `NetworkClientBase`, interfaces, message extensions. |
| `NanoPackets.Common` | netstandard2.0 | Attributes shared between runtime and generator (`[Packet]`, `[TransferExplicit]`). |
| `NanoPackets.Generator` | netstandard2.0 | Roslyn incremental source generator. |
| `NanoPackets.Godot` | net9.0 (Godot) | Godot-specific server/client bases and `Vector2`/`Vector3`/`Quaternion`/`Basis`/`Transform3D`/`Color` serialization helpers. |
| `NanoPackets.LoopTransport` | net8.0 | An in-process, queue-based Riptide transport (no sockets, no threads) - a server and client can run in one process, which is what makes the test suite possible, and what a singleplayer game can run its local server/client pair over. |
| `NanoPackets.SteamTransport` | net8.0 | A Riptide transport over the Steam Datagram Relay (Facepunch.Steamworks). |
| `NanoPackets.Example` | net9.0 | Minimal example wiring a server, client, world, and a packet. |
| `NanoPackets.Tests` | net9.0 | Headless xUnit suite (serialization, dispatch, send modes/lifecycle, generator diagnostics) running over `NanoPackets.LoopTransport`. |

## Defining a packet

Mark a `partial struct` with `[Packet(ordered, reliable)]` and implement `IServerbound<>` and/or
`IClientbound<>`. Public, non-`const` fields are serialized in declaration order.

```csharp
[Packet(ordered: false, reliable: true)]
public readonly ref partial struct ChatPacket : IServerbound<MyServer>, IClientbound<MyClient> {
    public readonly string Text;

    public void Serverbound(MyServer network, ushort player) { /* handle on server */ }
    public void Clientbound(MyClient network, int player) { /* handle on client */ }
}
```

The generator emits, per build:

- `PacketId` — an enum with one entry per packet (deterministically ordered, so ids are stable
  across builds and across separately-compiled client/server assemblies).
- `Write()` / `Read(Message)` serialization for each packet.
- The `HandlePacket` dispatch switch on your `partial` server/client classes.
- `Broadcast` / `BroadcastExcept` helpers on the server.

Use `[TransferExplicit]` on an integer field to send it with a fixed width instead of the default
variable-length encoding.

## Defining server and client

Derive `partial` classes from `NetworkServerBase<,,,>` / `NetworkClientBase<,,,>` (or the Godot
bases). Exactly one server and one client must be defined per assembly that references the
generator.

```csharp
public partial class MyServer : NetworkServerBase<World, Player, Player, NetPlayer> {
    public MyServer(World world, IServer transport, ushort port, ushort maxClientCount = 10)
        : base(world, transport, port, maxClientCount) { }
    // ...
}
```

## Threading

Network instances are **not** thread-safe. Riptide raises its callbacks on the thread that pumps
the peer (the thread calling `Server.Update()` / `Client.Update()`), and the internal collections
are unsynchronized — pump and use a given instance from a single thread only.

## Batching

`NetworkBase` used to carry a `Frame`/`QueueToFrame` accumulator meant to coalesce several
messages into one `BatchPacket` per tick, but nothing ever flushed it - it was dead code, and
`QueueToFrame` had a bug of its own (it called `msg.GetBool()`, a *read*, on an outgoing message
still under construction). It has been removed. `BatchPacket` itself still exists and can be
constructed and sent manually; automatic per-tick batching can come back, with tests proving a
batch round-trips and unpacks in order, if packet volume ever justifies the complexity.

## Building

```sh
dotnet build NanoPackets.sln -c Release
```

## Testing

```sh
dotnet test NanoPackets.Tests/NanoPackets.Tests.csproj
```

`NanoPackets.Tests` runs headless (no Godot dependency) over `NanoPackets.LoopTransport`. Generated
packet/handler code is emitted to disk under `obj/Generated` (`EmitCompilerGeneratedFiles` in the
test and example projects) so it can be read directly rather than inferred.

## License

NanoPackets is licensed under the GNU Lesser General Public License v3 (see `LICENSE.txt`).
