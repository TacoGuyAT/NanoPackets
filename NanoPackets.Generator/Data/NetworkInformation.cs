using Microsoft.CodeAnalysis;

namespace NanoPackets.Generator.Data;

public enum NetworkKind {
    Server,
    Client,
}

public struct NetworkInformation {
    public string Usings;
    public string Namespace;
    public string ClassLine;
    public string ClassIdent;
    public Location Location;
    public NetworkKind Kind;
}
