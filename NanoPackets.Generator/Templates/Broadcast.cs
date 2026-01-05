using System.Runtime.CompilerServices;
/* CLASS_LINE */{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Message AsClient(Message msg, ushort playerId) {
        var ordered = msg.SendMode == MessageSendMode.Notify;
        var reliable = ordered ? msg.GetBool() : msg.SendMode == MessageSendMode.Reliable;

        var result = new ClientboundPacket(ordered, reliable, playerId, msg).Write();
        msg.Release();
        return result;
    }

    /// <param name="msg">Will be wrapped as Clientbound packet</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Broadcast(Message msg, ushort senderId) => Send(AsClient(msg, senderId));

    /// <param name="msg">Will be wrapped as Clientbound packet</param>
    public void BroadcastExcept(Message msg, ushort senderId) {
        msg = AsClient(msg, senderId);
        var notify = msg.SendMode == MessageSendMode.Notify;
        if(notify) {
            messagesReferenceCount.Add(msg, Server.Clients.Length - 1);
            foreach(var client in Server.Clients) {
                if(client.Id == senderId) continue;
                if(!reliableMessages.ContainsKey(client.Id)) {
                    reliableMessages.Add(client.Id, []);
                }
                reliableMessages[client.Id].Add(client.Send(msg, false), msg);
            }
        } else {
            Server.SendToAll(msg);
        }
    }
}