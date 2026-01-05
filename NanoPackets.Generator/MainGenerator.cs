using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NanoPackets.Generator.Data;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NanoPackets.Generator;

[Generator]
public class MainGenerator : IIncrementalGenerator {
    static NetworkTypeInformation? server = null;
    static NetworkTypeInformation? client = null;
    static HashSet<string> serverBases = new();
    static HashSet<string> clientBases = new();
    static HashSet<string> serverHandlers = new();
    static HashSet<string> clientHandlers = new();
    static HashSet<string> packetNamespaces = new();
    static Dictionary<string, string> typeNameExtensions = new();
    static string extensions = string.Empty;

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var assemblies = context.CompilationProvider.Select((x, _) => x);
        context.RegisterSourceOutput(assemblies, static (ctx, src) => {
            extensions = string.Empty;
            MainGenerator.client = null;
            MainGenerator.server = null;
            serverBases.Clear();
            clientBases.Clear();
            serverHandlers.Clear();
            clientHandlers.Clear();
            packetNamespaces.Clear();
            typeNameExtensions.Clear();

            foreach(var x in src.ExternalReferences.Where(x => x.Display?.Contains("NanoPackets") ?? false)) {
                if(src.GetAssemblyOrModuleSymbol(x) is IAssemblySymbol symbol) {
                    LookForImplementations(symbol.GlobalNamespace);
                }
            }
            return;
        });

        var packets = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsPacket(s),
                transform: static (ctx, _) => PacketTransform(ctx)
            );

        var client = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsClient(s),
                transform: static (ctx, _) => NetworkTransform(ctx)
            );

        var server = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsServer(s),
                transform: static (ctx, _) => NetworkTransform(ctx)
            );

        context.RegisterSourceOutput(packets, ProcessPacket);
        context.RegisterSourceOutput(server, ProcessServer);
        context.RegisterSourceOutput(client, ProcessClient);
        context.RegisterSourceOutput(packets.Select((x, _) => x).Collect(), GeneratePackets);
        context.RegisterSourceOutput(server, GeneratePartialServer);
        context.RegisterSourceOutput(client, GeneratePartialClient);
    }

    private void GeneratePartialServer(SourceProductionContext context, NetworkInformation info) {
        if(server == null) {
            context.ReportDiagnostic(
                Diagnostic.Create(new DiagnosticDescriptor(
                    "NP0003",
                    "Couldn't find server",
                    "Generator failed to find server in the current project",
                    "codegen",
                    DiagnosticSeverity.Error,
                    true
                ), null)
            );
            return;
        }

        var usings = info.Usings;
        string classLines;
        if(!string.IsNullOrWhiteSpace(server.Value.Namespace)) {
            usings += $"using {server.Value.Namespace};\n";
            classLines = $"\nnamespace {server.Value.Namespace};\n{info.ClassLine}";
        } else {
            classLines = $"\n{info.ClassLine}";
        }

        foreach(var x in packetNamespaces) {
            if(x != null && !usings.Contains(x)) {
                usings += $"using {x};\n";
            }
        }

        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("NanoPackets.Generator.Templates.Broadcast.cs");
        var data = new byte[stream.Length];
        var memoryStream = new MemoryStream(data);
        stream.CopyTo(memoryStream);

        var split = Encoding.UTF8.GetString(data).Split(["/* CLASS_LINE */"], StringSplitOptions.None);
        usings += split[0];
        var result = usings + classLines + split[1];
        context.AddSource($"{(string.IsNullOrWhiteSpace(info.Namespace) ? "" : $"{info.Namespace}.")}{info.ClassIdent}Broadcast.g.cs", result);

        stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("NanoPackets.Generator.Templates.ServerHandlers.cs");
        data = new byte[stream.Length];
        memoryStream = new MemoryStream(data);
        stream.CopyTo(memoryStream);

        split = Encoding.UTF8.GetString(data).Split(["/* CLASS_LINE */"], StringSplitOptions.None);
        usings += split[0];
        result = usings + classLines + split[1].Replace("NetworkServer", info.ClassIdent).Replace("/* PACKET_HANDLERS */", string.Join("\n            ", serverHandlers));
        context.AddSource($"{(string.IsNullOrWhiteSpace(info.Namespace) ? "" : $"{info.Namespace}.")}{info.ClassIdent}Handlers.g.cs", result);
    }

    private void GeneratePartialClient(SourceProductionContext context, NetworkInformation info) {
        if(client == null) {
            context.ReportDiagnostic(
                Diagnostic.Create(new DiagnosticDescriptor(
                    "NP0004",
                    "Couldn't find client",
                    "Generator failed to find client in the current project",
                    "codegen",
                    DiagnosticSeverity.Error,
                    true
                ), null)
            );
            return;
        }

        var usings = info.Usings;
        string classLines;
        if(!string.IsNullOrWhiteSpace(client.Value.Namespace)) {
            usings += $"using {client.Value.Namespace};\n";
            classLines = $"\nnamespace {client.Value.Namespace};\n{info.ClassLine}";
        } else {
            classLines = $"\n{info.ClassLine}";
        }

        foreach(var x in packetNamespaces) {
            if(x != null && !usings.Contains(x)) {
                usings += $"using {x};\n";
            }
        }

        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("NanoPackets.Generator.Templates.ClientHandlers.cs");
        var data = new byte[stream.Length];
        var memoryStream = new MemoryStream(data);
        stream.CopyTo(memoryStream);

        var split = Encoding.UTF8.GetString(data).Split(["/* CLASS_LINE */"], StringSplitOptions.None);
        usings += split[0];
        var result = usings + classLines + split[1].Replace("NetworkClient", info.ClassIdent).Replace("/* PACKET_HANDLERS */", string.Join("\n            ", clientHandlers));
        context.AddSource($"{(string.IsNullOrWhiteSpace(info.Namespace) ? "" : $"{info.Namespace}.")}{info.ClassIdent}Handlers.g.cs", result);
    }

    static void AddPacketHandler(string packetId, string packet, bool clientbound, bool serverbound) {
        var line = $"PacketId.{packetId} => {packet}.Read,";
        if(clientbound) {
            clientHandlers.Add(line);
        }
        if(serverbound) {
            serverHandlers.Add(line);
        }
    }

    static void LookForImplementations(INamespaceSymbol ns) {
        var types = ns.GetTypeMembers();
        foreach(var type in types) {
            if(type.Name == "Extensions") {
                var me = type
                    .GetMembers()
                    .OfType<IMethodSymbol>();
                foreach(var m in me
                    .Where(x => x.Parameters.Length == 2 && x.Name != "Add" && !x.Parameters[1].Type.Name.EndsWith("[]"))
                ) {
                    if(!typeNameExtensions.ContainsKey(m.Parameters[1].Type.Name)) {
                        typeNameExtensions.Add(m.Parameters[1].Type.Name, m.Name[3..]);
                    }
                }
                extensions += $"using {type.ContainingNamespace.ToDisplayString()};\n";
            }

            if(Recursive(type, x => x.Name == "NetworkServerBase", x => x.BaseType)) {
                serverBases.Add(type.Name);
                continue;
            }
            if(Recursive(type, x => x.Name == "NetworkClientBase", x => x.BaseType)) {
                clientBases.Add(type.Name);
                continue;
            }
        }

        foreach(var child in ns.GetNamespaceMembers()) {
            LookForImplementations(child);
        }
    }

    public static void GeneratePackets(SourceProductionContext context, ImmutableArray<PacketInformation> packets) {
        var assembly = Assembly.GetExecutingAssembly();
        var internalPackets = assembly.GetManifestResourceNames().Where(x => x.Contains("NanoPackets.Generator.Packets")).Select(x => x.Split('.')[^2]);

        var source = new StringBuilder();

        source.AppendLine($"namespace NanoPackets;");
        source.AppendLine($"public enum PacketId {{");
        foreach(var packet in packets) {
            var id = packet.StructIdent.EndsWith("Packet") ? packet.StructIdent[..^6] : packet.StructIdent;
            packetNamespaces.Add(packet.Namespace);
            AddPacketHandler(id, packet.StructIdent, packet.Clientbound != null, packet.Serverbound != null);
            source.AppendLine($"    {id},");
        }
        packetNamespaces.Add("NanoPackets.Packets");
        foreach(var packet in internalPackets) {
            source.AppendLine($"    {packet[..^6]},");
        }
        source.AppendLine($"    Unknown");
        source.AppendLine($"}}");

        context.AddSource($"NanoPackets.PacketId.g.cs", source.ToString());

        if(client == null || server == null) {
            throw new Exception("Client or server are not defined");
        }

        string usingString = extensions;
        if(!string.IsNullOrWhiteSpace(client.Value.Namespace)) {
            usingString = $"using {client.Value.Namespace};\n";
        }
        if(!string.IsNullOrWhiteSpace(server.Value.Namespace) && client.Value.Namespace != server.Value.Namespace) {
            usingString += $"using {server.Value.Namespace};\n";
        }

        foreach(var packet in internalPackets) {
            var stream = assembly.GetManifestResourceStream($"NanoPackets.Generator.Packets.{packet}.cs");
            var data = new byte[stream.Length];
            var memoryStream = new MemoryStream(data);
            stream.CopyTo(memoryStream);

            var result = usingString + Encoding.UTF8.GetString(data).Replace("NetworkClient", client.Value.Name).Replace("NetworkServer", server.Value.Name);
            AddPacketHandler(packet[..^6], packet, result.Contains("Clientbound("), result.Contains("Serverbound("));
            context.AddSource($"NanoPackets.Packets.{packet}.g.cs", result);
        }
    }

    private static bool IsServer(SyntaxNode s) {
        if(
            s is ClassDeclarationSyntax _class &&
            _class.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword)) &&
            _class.BaseList is BaseListSyntax baseList &&
            baseList.Types.Select(x => x.Type).OfType<GenericNameSyntax>().Any(x => serverBases.Contains(x.Identifier.Text))
        ) {
            return true;
        }

        return false;
    }

    private static bool IsClient(SyntaxNode s) {
        if(
            s is ClassDeclarationSyntax _class &&
            _class.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword)) &&
            _class.BaseList is BaseListSyntax baseList &&
            baseList.Types.Select(x => x.Type).OfType<GenericNameSyntax>().Any(x => clientBases.Contains(x.Identifier.Text))
        ) {
            return true;
        }

        return false;
    }

    private static NetworkInformation NetworkTransform(GeneratorSyntaxContext ctx) {
        var _class = (ClassDeclarationSyntax)ctx.Node;

        NetworkInformation result = new() {
            Location = _class.GetLocation(),
            ClassIdent = _class.Identifier.Text,
        };

        result.ClassLine = $"{_class.Modifiers.ToFullString()}" +
            $"{_class.Keyword.ToFullString()}" +
            $"{result.ClassIdent}{(_class.TypeParameterList is null ? "" : _class.TypeParameterList.ToString())} " +
            $"{(_class.BaseList != null ? _class.BaseList.ToFullString() : "")}" +
            $"{_class.ConstraintClauses.ToFullString()}";

        var parent = _class.Parent;
        if(parent != null) {
            while(parent is not BaseNamespaceDeclarationSyntax) {
                if(parent.Parent == null) {
                    break;
                }
                parent = parent.Parent;
            }

            if(parent is BaseNamespaceDeclarationSyntax _namespace) {
                result.Namespace = _namespace.Name.ToString();
                if(_namespace.Parent != null) {
                    foreach(var child in _namespace.Parent.ChildNodes()) {
                        if(child is UsingDirectiveSyntax _using) {
                            result.Usings += _using.ToString();
                            result.Usings += '\n';
                        }
                    }
                }
            } else {
                foreach(var child in parent.ChildNodes()) {
                    if(child is UsingDirectiveSyntax _using) {
                        result.Usings += _using.ToString();
                        result.Usings += '\n';
                    }
                }
            }
        }

        return result;
    }

    private void ProcessServer(SourceProductionContext context, NetworkInformation info) {
        if(server != null && server.Value.Name != info.ClassIdent) {
            context.ReportDiagnostic(
                Diagnostic.Create(new DiagnosticDescriptor(
                    "NP0001",
                    "Multiple server definitions",
                    "Multiple servers can't be defined in the same assembly",
                    "codegen",
                    DiagnosticSeverity.Error,
                    true
                ), info.Location)
            );
        } else {
            server = new(info.ClassIdent, info.Namespace);
        }
    }

    private void ProcessClient(SourceProductionContext context, NetworkInformation info) {
        if(client != null && client.Value.Name != info.ClassIdent) {
            context.ReportDiagnostic(
                Diagnostic.Create(new DiagnosticDescriptor(
                    "NP0002",
                    "Multiple client definitions",
                    "Multiple client can't be defined in the same assembly",
                    "codegen",
                    DiagnosticSeverity.Error,
                    true
                ), info.Location)
            );
        } else {
            client = new(info.ClassIdent, info.Namespace);
        }
    }

    private static bool IsPacket(SyntaxNode s)
        => s is StructDeclarationSyntax _struct && 
        _struct.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword)) && // TODO: Report missing partial keyword diagnostics
        _struct.AttributeLists.Any(x => x.Attributes.Any(x => x.Name.ToString() == "Packet"));

    private static PacketInformation PacketTransform(GeneratorSyntaxContext ctx) {
        var _struct = (StructDeclarationSyntax)ctx.Node;
        if(_struct.AttributeLists.Select(x => x.Attributes.First(x => x.Name.ToString() == "Packet")).First() is AttributeSyntax attr) {
            var methods = _struct.Members.OfType<MethodDeclarationSyntax>();
            var v = _struct.Members.OfType<FieldDeclarationSyntax>().First().Modifiers;
            PacketInformation result = new() {
                Packet = _struct,
                Fields = _struct.Members.OfType<FieldDeclarationSyntax>()
                    .Where(x => x.Modifiers.Any(x => x.IsKind(SyntaxKind.PublicKeyword)) && !x.Modifiers.Any(x => x.IsKind(SyntaxKind.ConstKeyword)))
                    .SelectMany(x => x.Declaration.Variables.Select(t => new FieldInformation(
                        x.Declaration.Type.ToString(),
                        t.Identifier.Text,
                        x.AttributeLists.Any(x => x.Attributes.Any(x => x.Name.ToString() == "TransferExplicit"))
                    ))),
                StructIdent = _struct.Identifier.Text,
                // TODO: Check for interface and report diagnostics; there's more room for improvement afterwards
                Clientbound = methods.FirstOrDefault(x => x.Identifier.Text == "Clientbound")?.ParameterList.Parameters.First().Type!.ToString(),
                Serverbound = methods.FirstOrDefault(x => x.Identifier.Text == "Serverbound")?.ParameterList.Parameters.First().Type!.ToString(),
            };
            if(attr.ArgumentList is AttributeArgumentListSyntax list) {
                result.Ordered = list.Arguments[0].ToString() == "true";
                result.Reliable = list.Arguments[1].ToString() == "true";
            }
            result.StructLine = $"{_struct.Modifiers.ToFullString()}" +
                $"{_struct.Keyword.ToFullString()}" +
                $"{result.StructIdent}{(_struct.TypeParameterList is null ? "" : _struct.TypeParameterList.ToString())} " +
                $"{(_struct.BaseList != null ? _struct.BaseList.ToFullString() : "")}" +
                $"{_struct.ConstraintClauses.ToFullString()}";

            var parent = _struct.Parent;
            if(parent != null) {
                while(parent is not BaseNamespaceDeclarationSyntax) {
                    if(parent.Parent == null) {
                        break;
                    }
                    parent = parent.Parent;
                }

                if(parent is BaseNamespaceDeclarationSyntax _namespace) {
                    result.Namespace = _namespace.Name.ToString();
                    if(_namespace.Parent != null) {
                        foreach(var child in _namespace.Parent.ChildNodes()) {
                            if(child is UsingDirectiveSyntax _using) {
                                result.Usings += _using.ToString();
                                result.Usings += '\n';
                            }
                        }
                    }
                } else {
                    foreach(var child in parent.ChildNodes()) {
                        if(child is UsingDirectiveSyntax _using) {
                            result.Usings += _using.ToString();
                            result.Usings += '\n';
                        }
                    }
                }
            }


            return result;
        } else {
            throw new Exception("IsSyntaxTargetForGeneration output is incorrect");
        }
    }

    private void ProcessPacket(SourceProductionContext context, PacketInformation info) {
        if(!info.Fields.Any()) {
            return;
        }
        var source = new StringBuilder();
        var hasNamespace = !string.IsNullOrWhiteSpace(info.Namespace);

        source.AppendLine("using Riptide;");
        source.AppendLine(extensions[..^1]);
        source.AppendLine("using System.Runtime.InteropServices;");
        source.AppendLine("using System.Runtime.CompilerServices;");
        source.AppendLine(info.Usings);
        if(hasNamespace) {
            source.AppendLine($"namespace {info.Namespace};");
            source.AppendLine();
        }
        source.AppendLine($"[StructLayout(LayoutKind.Auto)]");
        source.AppendLine($"{info.StructLine}{{");

        var dynamicSendMode = info.Ordered == null;
        if(dynamicSendMode) {
            source.AppendLine($"    readonly bool ordered;");
            source.AppendLine($"    readonly bool reliable;");
        }

        var parameters = string.Join(", ", info.Fields.Select(x => $"{x.Type} {x.Name.ToCamelCase()}"));
        source.AppendLine($"    public {info.StructIdent}({(dynamicSendMode ? "bool ordered, bool reliable, " : "")}{parameters}) {{");
        if(dynamicSendMode) {
            source.AppendLine($"        this.ordered = ordered;");
            source.AppendLine($"        this.reliable = reliable;");
        }
        foreach(var field in info.Fields) {
            source.AppendLine($"        this.{field.Name} = {field.Name.ToCamelCase()};");
        }
        source.AppendLine($"    }}");
        if(dynamicSendMode) {
            source.AppendLine();
            source.AppendLine($"    {info.StructIdent}({parameters}) {{");
            foreach(var field in info.Fields) {
                source.AppendLine($"        this.{field.Name} = {field.Name.ToCamelCase()};");
            }
            source.AppendLine($"    }}");
        }
        source.AppendLine();

        var sendMode = dynamicSendMode
            ? "sendMode" :
            (info.Ordered == true ? "MessageSendMode.Notify" : (info.Reliable == true ? "MessageSendMode.Reliable" : "MessageSendMode.Unreliable"));
        source.AppendLine($"    public Message Write() {{");
        if(dynamicSendMode) {
            source.AppendLine($"        MessageSendMode sendMode;");
            source.AppendLine($"        if(ordered) {{");
            source.AppendLine($"            sendMode = MessageSendMode.Notify;");
            source.AppendLine($"        }} else if(reliable) {{");
            source.AppendLine($"            sendMode = MessageSendMode.Reliable;");
            source.AppendLine($"        }} else {{");
            source.AppendLine($"            sendMode = MessageSendMode.Unreliable;");
            source.AppendLine($"        }}");
            source.AppendLine();
        }
        source.AppendLine($"        var msg = Message.Create({sendMode}, PacketId.Batch);");
        if(dynamicSendMode) {
            source.AppendLine($"        if(ordered) {{");
            source.AppendLine($"            msg.AddBool(reliable);");
            source.AppendLine($"        }}");
        }
        source.AppendLine();
        foreach(var field in info.Fields) {
            source.AppendLine($"        msg.Add{IntoTypeName(field.Type, field.IsExplicit)}({field.Name});");
        }
        source.AppendLine();
        source.AppendLine($"        return msg;");
        source.AppendLine($"    }}");
        source.AppendLine();
        source.AppendLine($"    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        source.AppendLine($"    public static {info.StructIdent} Read(Message _msg) {{");
        if(dynamicSendMode) {
            source.AppendLine($"        if(_msg.SendMode == MessageSendMode.Notify) {{");
            source.AppendLine($"            _msg.GetBool();");
            source.AppendLine($"        }}");
            source.AppendLine();
        }
        foreach(var field in info.Fields) {
            source.AppendLine($"        {field.Type} {field.Name.ToCamelCase()} = _msg.Get{IntoTypeName(field.Type, field.IsExplicit)}();");
        }
        source.AppendLine();
        source.AppendLine($"        return new {info.StructIdent}({string.Join(", ", info.Fields.Select(x => x.Name.ToCamelCase()))});");
        source.AppendLine($"    }}");

        if(info.Clientbound is string client) {
            source.AppendLine($"    public static void Read(Message _msg, {client} _network, int _player) => Read(_msg).Clientbound(_network, _player);");
        }

        if(info.Serverbound is string server) {
            source.AppendLine($"    public static void Read(Message _msg, {server} _network, ushort _player) => Read(_msg).Serverbound(_network, _player);");
        }

        source.AppendLine($"}}");

        context.AddSource($"{(string.IsNullOrWhiteSpace(info.Namespace) ? "" : $"{info.Namespace}.")}{info.StructIdent}.g.cs", source.ToString());
    }

    static bool Recursive<T>(T symbol, Func<T, bool> check, Func<T, T?> next)
        => (check.Invoke(symbol) || (next.Invoke(symbol) is T result && Recursive(result, check, next)));

    public string IntoTypeName(string type, bool isExplicit) {
        var isArray = false;
        if(type.EndsWith("[]")) {
            type = type[..^2];
            isArray = true;
        }
        string result;
        if(isExplicit) {
            result = type switch {
                "sbyte" => "SByte",
                "byte" => "Byte",
                "short" => "Short",
                "ushort" => "UShort",
                "int" => "Int",
                "uint" => "UInt",
                "long" => "Long",
                "ulong" => "ULong",
                _ => IntoGetBase(type, ref isArray)
            };
        } else {
            switch(type) {
                case "sbyte":
                case "byte":
                case "short":
                case "ushort":
                case "int":
                case "uint":
                case "long":
                case "ulong":
                    result = "VarULong";
                    if(isArray) {
                        result += $"s<{type}>";
                        isArray = false;
                    } else {
                        result += $"<{type}>";
                    }
                    break;
                default:
                    result = IntoGetBase(type, ref isArray);
                    break;
            }
        }
        if(isArray) {
            result += 's';
        }
        return result;
    }

    public string IntoGetBase(string type, ref bool isArray) {
        var result = type switch {
            "bool" => "Bool",
            "string" => "String",
            _ => null
        };
        if(result == null && !typeNameExtensions.TryGetValue(type, out result)) { 
            if(isArray) {
                result = $"Serializables<{type}>";
                isArray = false;
            } else {
                result = $"Serializable<{type}>";
            }
        }
        return result;
    }
}
