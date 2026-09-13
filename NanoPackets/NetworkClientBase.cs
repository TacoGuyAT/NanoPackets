using Riptide;
using Riptide.Transports;
using Riptide.Utils;

namespace NanoPackets;
public abstract class NetworkClientBase<TWorld, TPlayerBase, TPlayer, TNetPlayer> : NetworkBase<TWorld, TPlayerBase, TPlayer>
    where TWorld : IWorld<TPlayer>
    where TPlayer : TPlayerBase
    where TNetPlayer : TPlayerBase, INetPlayer
{
    public Client Client => (Client)Peer;
    protected Dictionary<ushort, Message> reliableMessages = [];

    /// <summary>Ensures <see cref="OnDisconnect"/> is raised exactly once, whether the disconnect
    /// originates from us calling <see cref="Disconnect"/> or from the transport.</summary>
    private bool disconnectNotified;

    public NetworkClientBase(TWorld world, IClient transport, string addr) : base(world, new Client(transport)) {
        Client.ClientDisconnected += (s, e) => {
            // Only networked players (TNetPlayer) get the disconnect callback; the local player is a
            // plain TPlayer and must not be force-cast.
            if(Players.Remove(e.Id, out var p) && p is TNetPlayer netPlayer) {
                netPlayer.NetHandleDisconnect();
            }
        };
        Client.Disconnected += (s, e) => {
            // Transport-initiated disconnect (timed out, kicked, server stopped, ...). Protocol-level
            // disconnects that carry a DisconnectCode/reason are surfaced via Disconnect() instead;
            // the guard in NotifyDisconnect keeps this from firing OnDisconnect a second time.
            NotifyDisconnect(MapDisconnectReason(e.Reason), e.Reason.ToString());
        };
        Client.MessageReceived += (s, e) => {
            HandlePacket(e.MessageId, e.Message, -1);
        };
        Client.Connected += (s, e) => {
            RiptideLogger.Log(LogType.Debug, $"Client: Connection id is {Client.Connection.Id}");
            Players.Add(Client.Connection.Id, Player);
            Client.Connection.NotifyDelivered += (id) => {
                reliableMessages.Remove(id);
            };
            Client.Connection.NotifyLost += (id) => {
                // Always drop the old sequence id: on a reliable resend Send() re-registers the
                // message under a new id, so leaving the old entry in place leaked it.
                if(reliableMessages.Remove(id, out var msg)) {
                    msg.GetVarULong();
                    if(msg.GetBool()) {
                        Send(msg);
                    }
                }
            };
            Client.Connection.NotifyReceived += (msg) => {
                HandlePacket((ushort)msg.GetVarULong(), msg, -1);
            };
        };

        Client.Connect($"{addr}", 5, 0, null, false);
    }

    public abstract void HandlePacket(ushort msgId, Message msg, int playerId);

    /// <param name="msg">Must be a Serverbound packet</param>
    public override void Send(Message msg) {
        if(msg.SendMode == MessageSendMode.Notify) {
            var id = Client.Send(msg, false);
            reliableMessages.Add(id, msg);
        } else {
            Client.Send(msg);
        }
    }

    public void Disconnect(DisconnectCode code, string reason) {
        RiptideLogger.Log(LogType.Error, $"Client: {code} - {reason}");
        NotifyDisconnect(code, reason);
        Client.Disconnect();
    }

    /// <summary>Raises <see cref="OnDisconnect"/> at most once for this client.</summary>
    private void NotifyDisconnect(DisconnectCode code, string reason) {
        if(disconnectNotified) {
            return;
        }
        disconnectNotified = true;
        OnDisconnect(code, reason);
    }

    private static DisconnectCode MapDisconnectReason(DisconnectReason reason) => reason switch {
        // The NanoPackets codes describe protocol-level failures; transport reasons don't map onto
        // them, so they surface as Generic with the Riptide reason carried in the reason string.
        _ => DisconnectCode.Generic,
    };

    protected abstract void OnDisconnect(DisconnectCode code, string reason);
}
