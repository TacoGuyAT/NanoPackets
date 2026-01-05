namespace NanoPackets.Data;

public enum DisconnectCode : byte {
    None = 0,
    Generic = 1,
    OutdatedClient = 2,
    OutdatedServer = 3,
    IncorrectPacketSequence = 4,
}
