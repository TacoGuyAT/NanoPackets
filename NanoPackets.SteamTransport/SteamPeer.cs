using System.Runtime.InteropServices;
using Riptide;
using Riptide.Transports;
using Steamworks;
using DisconnectedEventArgs = Riptide.Transports.DisconnectedEventArgs;

namespace NanoPackets.SteamTransport;

/// <summary>Provides base functionality shared by the Steam <see cref="SteamServer"/> and <see cref="SteamClient"/> transports.</summary>
public abstract class SteamPeer : IPeer {
    /// <inheritdoc/>
    public event EventHandler<DataReceivedEventArgs>? DataReceived;
    /// <inheritdoc/>
    public event EventHandler<DisconnectedEventArgs>? Disconnected;

    /// <summary>The maximum number of messages to read from the Steam networking layer per <see cref="Poll"/> call.</summary>
    protected const int MaxMessagesPerPoll = 256;

    /// <summary>A reusable buffer used to marshal incoming (unmanaged) Steam messages into managed memory.</summary>
    private byte[] receiveBuffer = new byte[1500];

    /// <inheritdoc/>
    public abstract void Poll();

    /// <summary>Copies an incoming Steam message into managed memory and raises <see cref="DataReceived"/>.</summary>
    /// <param name="data">A pointer to the unmanaged message data supplied by Steam.</param>
    /// <param name="size">The length of the message, in bytes.</param>
    /// <param name="fromConnection">The connection the message was received on.</param>
    protected void HandleMessage(IntPtr data, int size, SteamConnection fromConnection) {
        if (size <= 0)
            return;

        if (receiveBuffer.Length < size)
            receiveBuffer = new byte[size];

        Marshal.Copy(data, receiveBuffer, 0, size);
        DataReceived?.Invoke(this, new DataReceivedEventArgs(receiveBuffer, size, fromConnection));
    }

    /// <summary>Raises the <see cref="Disconnected"/> event.</summary>
    /// <param name="connection">The connection that was disconnected.</param>
    /// <param name="reason">The reason for the disconnection.</param>
    protected void RaiseDisconnected(SteamConnection connection, DisconnectReason reason) =>
        Disconnected?.Invoke(this, new DisconnectedEventArgs(connection, reason));

    /// <summary>Maps a Steam <see cref="NetConnectionEnd"/> reason onto the closest Riptide <see cref="DisconnectReason"/>.</summary>
    protected static DisconnectReason GetDisconnectReason(NetConnectionEnd endReason) => endReason switch {
        NetConnectionEnd.Remote_Timeout or NetConnectionEnd.Misc_Timeout => DisconnectReason.TimedOut,
        >= NetConnectionEnd.App_Min and <= NetConnectionEnd.App_Max => DisconnectReason.Disconnected,
        _ => DisconnectReason.TransportError,
    };
}
