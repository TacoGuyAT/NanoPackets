using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace NanoPackets.Generator.Data;

public struct PacketInformation {
    public List<FieldInformation> Fields;
    public string Usings;
    public string Namespace;
    public string StructIdent;
    public string StructLine;
    public bool Partial;
    public bool? Ordered;
    public bool? Reliable;
    public string? Clientbound;
    public string? Serverbound;
    public Location Location;
}
