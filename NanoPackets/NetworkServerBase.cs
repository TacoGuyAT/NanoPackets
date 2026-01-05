using Riptide;
using Riptide.Transports;
using Riptide.Utils;
using System.Runtime.CompilerServices;

namespace NanoPackets;
public abstract class NetworkServerBase<TWorld, TPlayerBase, TPlayer, TNetPlayer> : NetworkBase<TWorld, TPlayerBase, TPlayer>
    where TWorld : IWorld<TPlayer>
    where TPlayer : TPlayerBase
    where TNetPlayer : TPlayerBase, INetPlayer
{
    protected abstract TNetPlayer NewPlayer(ushort id);
    protected Dictionary<ushort, Dictionary<ushort, Message>> reliableMessages = [];
    protected Dictionary<Message, int> messagesReferenceCount = [];
    public Server Server => (Server)Peer;
    public NetworkServerBase(TWorld world, IServer transport, ushort port) : base(world, new Server(transport)) {
        Players.Add(-1, world.Player);

        Server.ClientConnected += (s, e) => {
            var newPlayer = NewPlayer(e.Client.Id);
            newPlayer.NetHandleConnect(e.Client.Id);
            e.Client.NotifyDelivered += (id) => {
                var senderId = e.Client.Id;
                if(!reliableMessages.ContainsKey(senderId)) {
                    reliableMessages.Add(senderId, []);
                } else if(reliableMessages[senderId].TryGetValue(id, out var msg)) {
                    messagesReferenceCount[msg] -= 1;
                    if(messagesReferenceCount[msg] == 0) {
                        messagesReferenceCount.Remove(msg);
                        msg.Release();
                    }
                } else {
                    throw new Exception("NotifyDelivered event was received for an unregistered message");
                }
            };
            e.Client.NotifyLost += (id) => {
                if(reliableMessages.TryGetValue(e.Client.Id, out var msgs)) {
                    if(msgs.Remove(id, out var msg)) {
                        msg.GetVarULong();
                        if(msg.GetBool()) {
                            msgs.Add(e.Client.Send(msg, false), msg);
                        } else {
                            messagesReferenceCount.Remove(msg);
                            msg.Release();
                        }
                    }
                } else {
                    // TODO: shutdown server
                    if(Players.TryGetValue(e.Client.Id, out var player)) { 
                        RiptideLogger.Log(LogType.Error, $"Server: Notify message lost for {player}. Desync?");
                    } else {
                        RiptideLogger.Log(LogType.Error, $"Server: Notify message lost for an unknown player (ID #{e.Client.Id}). Desync?");
                    }
                }
            };
            e.Client.NotifyReceived += (msg) => {
                HandlePacket((ushort)msg.GetVarULong(), msg, e.Client.Id);
            };
        };
        Server.ClientDisconnected += (s, e) => {
            Players.Remove(e.Client.Id, out var p);
            ((TNetPlayer)p!)!.NetHandleDisconnect(); // TODO: handle null and cast? should be impossible to hit
        };
        Server.MessageReceived += (s, e) => {
            HandlePacket(e.MessageId, e.Message, e.FromConnection.Id);
        };

        Server.Start(port, 1, 0, false);
    }

    public abstract void HandlePacket(ushort msgId, Message msg, ushort playerId);

    /// <param name="msg">Must be host's packet</param>
    public override void Send(Message msg) {
        var notify = msg.SendMode == MessageSendMode.Notify;
        if(notify) {
            messagesReferenceCount.Add(msg, Server.Clients.Length);
            foreach(var client in Server.Clients) {
                if(!reliableMessages.ContainsKey(client.Id)) {
                    reliableMessages.Add(client.Id, []);
                }
                reliableMessages[client.Id].Add(client.Send(msg, false), msg);
            }
        } else {
            Server.SendToAll(msg);
        }
    }

    /// <summary>
    /// Disconnects remote player from server
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Disconnect(TNetPlayer player, string reason = "Disconnected") => Disconnect((ushort)player.Id, reason);

    /// <summary>
    /// Disconnects remote player from server
    /// </summary>
    public abstract void Disconnect(ushort id, string reason = "Disconnected");
}
