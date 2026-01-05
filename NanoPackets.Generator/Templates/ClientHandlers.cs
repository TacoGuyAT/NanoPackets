using System;
using Riptide;
using Riptide.Utils;
using NanoPackets;
/* CLASS_LINE */{
    public override void HandlePacket(ushort msgId, Message msg, int playerId) {
        Action<Message, NetworkClient, int>? method = (PacketId)msgId switch {
            /* PACKET_HANDLERS */
            _ => null
        };

        RiptideLogger.Log(LogType.Debug, $"Client: Received {msgId:X4}:{(PacketId)msgId}");

        if(method is Action<Message, NetworkClient, int> m) {
            m.Invoke(msg, this, playerId);
        } else {
            Disconnect(DisconnectCode.IncorrectPacketSequence, $"Client: Unexpected packet ({msgId:X4}:{(PacketId)msgId}) received");
        }
    }
}