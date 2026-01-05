using Riptide;
using NanoPackets;
using NanoPackets.Data;
using System.Runtime.CompilerServices;

namespace NanoPackets.Packets;

[Packet(false, true)]
public readonly ref struct DisconnectPacket : IClientbound<NetworkClient> {
    public readonly DisconnectCode Code = DisconnectCode.Generic;
    public readonly string Reason;

    public void Clientbound(NetworkClient network, int player) {
        network.Disconnect(Code, Reason);
    }

    // TODO: autogenerate \/

    public DisconnectPacket(string reason) {
        this.Reason = reason;
    }

    public DisconnectPacket(DisconnectCode code, string reason) {
        this.Code = code;
        this.Reason = reason;
    }

    public Message Write() {
        var msg = Message.Create(MessageSendMode.Reliable, PacketId.Disconnect);

        msg.AddVarULong((ulong)Code);
        msg.AddString(Reason);

        return msg;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DisconnectPacket Read(Message _msg) {
        var code = (DisconnectCode)_msg.GetVarULong();
        var reason = _msg.GetString();

        return new DisconnectPacket(code, reason);
    }

    public static void Read(Message _msg, NetworkClient _network, int _player) => Read(_msg).Clientbound(_network, _player);
}
