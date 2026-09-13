using Godot;
using Riptide.Transports;

namespace NanoPackets.Godot;
public abstract class GodotNetworkServerBase<TWorld, TPlayerBase, TPlayer, TNetPlayer> : NetworkServerBase<TWorld, TPlayerBase, TPlayer, TNetPlayer>
    where TWorld : Node, IWorld<TPlayer>
    where TPlayerBase : Node
    where TPlayer : TPlayerBase
    where TNetPlayer : TPlayerBase, INetPlayer 
{
    protected readonly PackedScene NewServerPlayer;

    /// <param name="newServerPlayer">
    /// The player scene to instantiate for each connecting client, e.g. via <c>GD.Load&lt;PackedScene&gt;("res://...")</c>.
    /// Not an [Export]: this class isn't a Node (it inherits <see cref="NetworkServerBase{TWorld, TPlayerBase, TPlayer, TNetPlayer}"/>,
    /// and Godot requires a single class hierarchy rooted in Node for editor-assigned exports), so
    /// resolving the scene is the caller's responsibility, the same way <typeparamref name="TWorld"/>
    /// pumps <see cref="NetworkServerBase{TWorld, TPlayerBase, TPlayer, TNetPlayer}.Server"/> from its own _PhysicsProcess.
    /// </param>
    protected GodotNetworkServerBase(TWorld world, IServer transport, ushort port, PackedScene newServerPlayer, ushort maxClientCount = 10)
        : base(world, transport, port, maxClientCount) {
        NewServerPlayer = newServerPlayer;
    }
    protected override TNetPlayer NewPlayer(ushort id) {
        var newPlayer = NewServerPlayer.Instantiate<TNetPlayer>();
        Players.Add(id, newPlayer);
        World.AddChild(newPlayer);
        return newPlayer;
    }
}
