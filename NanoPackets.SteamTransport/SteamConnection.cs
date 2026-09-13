using Riptide.Transports;
using Steamworks;
using NetConnection = Steamworks.Data.Connection;
using SendType = Steamworks.Data.SendType;

namespace NanoPackets.SteamTransport;

/// <summary>Represents a connection to a Steam peer, tunnelled over the Steam Datagram Relay.</summary>
public class SteamConnection : Riptide.Connection, IEquatable<SteamConnection> {
    /// <summary>The Steam ID of the peer at the other end of this connection.</summary>
    public readonly SteamId SteamId;

    /// <summary>The underlying Steam networking connection handle.</summary>
    internal readonly NetConnection SteamNetConnection;

    /// <summary>Riptide stores the message header in the low 4 bits of the first byte (see <c>Riptide.Message.HeaderBitmask</c>).</summary>
    private const byte HeaderBitmask = 0b_1111;

    internal SteamConnection(SteamId steamId, NetConnection netConnection) {
        SteamId = steamId;
        SteamNetConnection = netConnection;
    }

    /// <inheritdoc/>
    protected override void Send(byte[] dataBuffer, int amount) {
        // Map Riptide's send mode onto a Steam send type: unreliable/notify messages are sent unreliably and rely on
        // Riptide's own reliability layer, while everything else is delivered reliably and in order by Steam.
        SendType sendType = (MessageHeader)(dataBuffer[0] & HeaderBitmask) switch {
            MessageHeader.Unreliable or MessageHeader.Notify => SendType.Unreliable,
            _ => SendType.Reliable,
        };

        // The result is intentionally ignored: sends that occur before the relay connection is fully established fail
        // harmlessly, and Riptide retransmits its reliable handshake messages until the connection is ready.
        SteamNetConnection.SendMessage(dataBuffer, 0, amount, sendType);
    }

    /// <summary>Closes the underlying Steam networking connection.</summary>
    internal void Close() => SteamNetConnection.Close();

    /// <inheritdoc/>
    public override string ToString() => $"Steam connection to {SteamId}";

    /// <inheritdoc/>
    public bool Equals(SteamConnection? other) => other is not null && SteamNetConnection.Id == other.SteamNetConnection.Id;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as SteamConnection);

    /// <inheritdoc/>
    public override int GetHashCode() => SteamNetConnection.Id.GetHashCode();
}
