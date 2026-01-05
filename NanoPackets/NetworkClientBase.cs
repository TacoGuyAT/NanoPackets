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
    public NetworkClientBase(TWorld world, IClient transport, string addr) : base(world, new Client(transport)) {
        Client.ClientDisconnected += (s, e) => {
            Players.Remove(e.Id, out var p);
            ((TNetPlayer)p!)!.NetHandleDisconnect(); // TODO: handle null and cast? should be impossible to hit
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
                if(reliableMessages.TryGetValue(id, out var msg)) {
                    msg.GetVarULong();
                    if(msg.GetBool()) {
                        Send(msg);
                    } else {
                        reliableMessages.Remove(id);
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
        Client.Disconnect();
    }

    protected abstract void OnDisconnect(DisconnectCode code, string reason);
}
