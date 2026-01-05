using Godot;
using Riptide.Transports;

namespace NanoPackets.Godot;
public abstract class GodotNetworkClientBase<TWorld, TPlayerBase, TPlayer, TNetPlayer> : NetworkClientBase<TWorld, TPlayerBase, TPlayer, TNetPlayer>
    where TWorld : Node, IWorld<TPlayer>
    where TPlayerBase : Node
    where TPlayer : TPlayerBase
    where TNetPlayer : TPlayerBase, INetPlayer {
    protected GodotNetworkClientBase(TWorld world, IClient transport, string addr) : base(world, transport, addr) { }
}
