using Godot;
using Riptide.Transports;

namespace NanoPackets.Godot;
public abstract class GodotNetworkServerBase<TWorld, TPlayerBase, TPlayer, TNetPlayer> : NetworkServerBase<TWorld, TPlayerBase, TPlayer, TNetPlayer>
    where TWorld : Node, IWorld<TPlayer>
    where TPlayerBase : Node
    where TPlayer : TPlayerBase
    where TNetPlayer : TPlayerBase, INetPlayer 
{
    [Export]
    protected PackedScene NewServerPlayer;
    protected GodotNetworkServerBase(TWorld world, IServer transport, ushort port) : base(world, transport, port) { }
    protected override TNetPlayer NewPlayer(ushort id) {
        var newPlayer = NewServerPlayer.Instantiate<TNetPlayer>();
        Players.Add(id, newPlayer);
        World.AddChild(newPlayer);
        return newPlayer;
    }
}
