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
| `NanoPackets.Godot` | net9.0 (Godot) | Godot-specific server/client bases and `Vector2`/`Vector3` serialization helpers. |
| `NanoPackets.Example` | net9.0 | Minimal example wiring a server, client, world, and a packet. |

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

## Building

```sh
dotnet build NanoPackets.sln -c Release
```

## License

NanoPackets is licensed under the GNU Affero General Public License v3 (see `LICENSE.txt`). Note
that the AGPL is strongly copyleft; review its terms before embedding NanoPackets in a closed or
network-served application.
