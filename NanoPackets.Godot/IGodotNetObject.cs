using Godot;
using NanoPackets.Godot.Utils;
using Riptide;

namespace NanoPackets.Godot;
public interface IGodotNetObject : INetObject {
    Node3D node { get; }
    void INetObject.Serialize(Message msg) {
        msg.AddVector3(node.Rotation);
        msg.AddVector3(node.GlobalPosition);
    }

    void INetObject.Deserialize(Message msg) {
        node.Rotation = msg.GetVector3();
        node.GlobalPosition = msg.GetVector3();
    }
}
