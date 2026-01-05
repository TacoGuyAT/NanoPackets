using System;
using Riptide;
using Riptide.Utils;
using NanoPackets.Data;
/* CLASS_LINE */{
    public override void HandlePacket(ushort msgId, Message msg, ushort playerId) {
        Action<Message, NetworkServer, ushort>? method = (PacketId)msgId switch {
            /* PACKET_HANDLERS */
            _ => null
        };

        RiptideLogger.Log(LogType.Debug, $"Server: Received {msgId:X4}:{(PacketId)msgId}");

        if(method is Action<Message, NetworkServer, ushort> m) {
            m.Invoke(msg, this, playerId);
        } else {
            var reason = $"Unexpected packet ({msgId:X4}:{(PacketId)msgId}) received";
            RiptideLogger.Log(LogType.Error, $"Server: Player_{playerId:0000} - {reason}");
            Server.DisconnectClient(playerId, new DisconnectPacket(reason).Write());
        }
    }
}