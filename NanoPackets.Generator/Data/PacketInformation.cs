using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace NanoPackets.Generator.Data;

public struct PacketInformation {
    public StructDeclarationSyntax Packet;
    public IEnumerable<FieldInformation> Fields;
    public string Usings;
    public string Namespace;
    public string StructIdent;
    public string StructLine;
    public bool? Ordered;
    public bool? Reliable;
    public string? Clientbound;
    public string? Serverbound;
}
