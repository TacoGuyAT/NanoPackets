using Riptide;
using NanoPackets;
using NanoPackets.Data;
using System.Runtime.CompilerServices;

namespace NanoPackets.Packets;

[Packet]
public readonly ref struct BatchPacket : IClientbound<NetworkClient>, IServerbound<NetworkServer> {
    public readonly Message[] Messages;

    public void Clientbound(NetworkClient network, int player) {
        var msg = Messages[0];
        for(int i = 0; i < Messages.Length; i++) {
            var id = (ushort)msg.GetVarULong();
            network.HandlePacket(id, msg, player);
        }
    }

    public void Serverbound(NetworkServer network, ushort player) {
        var msg = Messages[0];
        for(int i = 0; i < Messages.Length; i++) {
            var id = (ushort)msg.GetVarULong();
            network.HandlePacket(id, msg, player);
        }
    }

    // TODO: autogenerate \/

    readonly bool ordered;
    readonly bool reliable;
    public BatchPacket(bool ordered, bool reliable, Message[] messages) {
        this.ordered = ordered;
        this.reliable = reliable;
        this.Messages = messages;
    }

    BatchPacket(Message[] messages) {
        this.Messages = messages;
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

        var msg = Message.Create(sendMode, PacketId.Batch);
        if(ordered) {
            msg.AddBool(reliable);
        }

        msg.AddVarULong((ulong)Messages.Length);
        foreach(var message in Messages) {
            msg.AddMessage(message);
        }

        return msg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BatchPacket Read(Message _msg) {
        if(_msg.SendMode == MessageSendMode.Notify) {
            _msg.GetBool();
        }

        var messages = new Message[(int)_msg.GetVarULong()];
        messages[0] = _msg;

        return new BatchPacket(messages);
    }

    public static void Read(Message _msg, NetworkClient _network, int _player) => Read(_msg).Clientbound(_network, _player);

    public static void Read(Message _msg, NetworkServer _network, ushort _player) => Read(_msg).Serverbound(_network, _player);
}
