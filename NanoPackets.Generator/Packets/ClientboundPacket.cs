using Riptide;
using NanoPackets;
using System.Runtime.CompilerServices;

namespace NanoPackets.Packets;

[Packet]
public readonly ref struct ClientboundPacket : IClientbound<NetworkClient> {
    public readonly ushort Player;
    public readonly Message Message;

    public void Clientbound(NetworkClient network, int _) {
        var innerPacketId = (PacketId)Message.GetVarULong();
        if(innerPacketId == PacketId.Clientbound) {
            network.Disconnect(DisconnectCode.IncorrectPacketSequence, "Clientbound cannot contain Clientbound packet");
        }
        network.HandlePacket((ushort)innerPacketId, Message, Player);
    }

    // TODO: autogenerate \/

    readonly bool ordered;
    readonly bool reliable;
    public ClientboundPacket(bool ordered, bool reliable, ushort player, Message message) {
        this.ordered = ordered;
        this.reliable = reliable;
        Player = player;
        Message = message;
    }

    ClientboundPacket(ushort player, Message message) {
        Player = player;
        Message = message;
    }

    public Message Write() {
        MessageSendMode sendMode;
        if(ordered) {
            sendMode = MessageSendMode.Notify;
        } else if(reliable) {
            sendMode = MessageSendMode.Reliable;
        } else {
            sendMode = MessageSendMode.Unreliable;
        }

        var msg = Message.Create(sendMode, PacketId.Clientbound);
        if(ordered) {
            msg.AddBool(reliable);
        }

        msg.AddVarULong(Player);
        msg.AddMessage(Message);

        return msg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ClientboundPacket Read(Message _msg) {
        if(_msg.SendMode == MessageSendMode.Notify) {
            _msg.GetBool();
        }

        var player = (ushort)_msg.GetVarULong();
        var message = _msg;

        return new ClientboundPacket(player, message);
    }

    public static void Read(Message _msg, NetworkClient _network, int _player) => Read(_msg).Clientbound(_network, _player);

}