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
    public NetworkServerBase(TWorld world, IServer transport, ushort port, ushort maxClientCount = 10) : base(world, new Server(transport)) {
        Players.Add(-1, world.Player);

        Server.ClientConnected += (s, e) => {
            var newPlayer = NewPlayer(e.Client.Id);
            newPlayer.NetHandleConnect(e.Client.Id);
            e.Client.NotifyDelivered += (id) => {
                // Drop the per-client entry for the acked sequence id (it was never removed before,
                // so these maps grew without bound) and release the shared message once every
                // recipient has acked or given up. Never throw from a transport callback.
                if(reliableMessages.TryGetValue(e.Client.Id, out var msgs) && msgs.Remove(id, out var msg)) {
                    ReleaseReference(msg);
                } else {
                    RiptideLogger.Log(LogType.Warning, $"Server: NotifyDelivered for an untracked message (client {e.Client.Id}, id {id})");
                }
            };
            e.Client.NotifyLost += (id) => {
                if(reliableMessages.TryGetValue(e.Client.Id, out var msgs) && msgs.Remove(id, out var msg)) {
                    msg.GetVarULong();
                    if(msg.GetBool()) {
                        // Reliable: retransmit to this client. The message stays outstanding, so its
                        // reference count is unchanged (this pending entry is replaced by the new one).
                        msgs.Add(e.Client.Send(msg, false), msg);
                    } else {
                        // Unreliable: give up for this client. The same Message instance is shared
                        // across all recipients, so only release it once none still has it in flight
                        // (previously it was released here unconditionally, freeing it while other
                        // clients still referenced it).
                        ReleaseReference(msg);
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
            // The local/host player lives at key -1 and is a TPlayer, not necessarily a TNetPlayer;
            // only notify entries that are actually networked players to avoid an invalid cast.
            if(Players.Remove(e.Client.Id, out var p) && p is TNetPlayer netPlayer) {
                netPlayer.NetHandleDisconnect();
            }
        };
        Server.MessageReceived += (s, e) => {
            HandlePacket(e.MessageId, e.Message, e.FromConnection.Id);
        };

        Server.Start(port, maxClientCount, 0, false);
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
    /// Decrements the outstanding-recipient count for a notify message and releases it back to the
    /// pool once no recipient still has it in flight.
    /// </summary>
    private void ReleaseReference(Message msg) {
        if(!messagesReferenceCount.TryGetValue(msg, out var count)) {
            return;
        }
        if(count <= 1) {
            messagesReferenceCount.Remove(msg);
            msg.Release();
        } else {
            messagesReferenceCount[msg] = count - 1;
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
