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
    // NOTE: This generator intentionally holds NO mutable static state. Everything discovered is
    // threaded through the incremental pipeline as values and merged into a single source-output
    // step, so every emitted file sees a complete, consistent snapshot regardless of the order in
    // which Roslyn runs the pipeline (the previous static-field design produced empty handler
    // switches / missing usings depending on that order, and leaked state between projects).

    static readonly DiagnosticDescriptor MultipleServers = new(
        "NP0001", "Multiple server definitions",
        "Multiple servers can't be defined in the same assembly", "codegen", DiagnosticSeverity.Error, true);
    static readonly DiagnosticDescriptor MultipleClients = new(
        "NP0002", "Multiple client definitions",
        "Multiple clients can't be defined in the same assembly", "codegen", DiagnosticSeverity.Error, true);
    static readonly DiagnosticDescriptor MissingServer = new(
        "NP0003", "Couldn't find server",
        "Generator failed to find a server in the current project", "codegen", DiagnosticSeverity.Error, true);
    static readonly DiagnosticDescriptor MissingClient = new(
        "NP0004", "Couldn't find client",
        "Generator failed to find a client in the current project", "codegen", DiagnosticSeverity.Error, true);
    static readonly DiagnosticDescriptor PacketWithoutFields = new(
        "NP0005", "Packet has no serializable fields",
        "Packet '{0}' has no public, non-const fields and will be skipped", "codegen", DiagnosticSeverity.Warning, true);
    static readonly DiagnosticDescriptor PacketNotPartial = new(
        "NP0006", "Packet must be partial",
        "Packet '{0}' must be declared 'partial' for serialization code to be generated", "codegen", DiagnosticSeverity.Error, true);

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var packets = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsPacketCandidate(s),
                transform: static (ctx, _) => PacketTransform(ctx))
            .Where(static x => x is not null)
            .Select(static (x, _) => x!.Value);

        var networks = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsNetworkCandidate(s),
                transform: static (ctx, _) => NetworkTransform(ctx))
            .Where(static x => x is not null)
            .Select(static (x, _) => x!.Value);

        var extensions = context.CompilationProvider.Select(static (c, _) => CollectExtensions(c));

        var combined = packets.Collect()
            .Combine(networks.Collect())
            .Combine(extensions);

        context.RegisterSourceOutput(combined, static (ctx, data) =>
            Emit(ctx, data.Left.Left, data.Left.Right, data.Right));
    }

    // ---- Discovery --------------------------------------------------------------------------

    private static bool IsPacketCandidate(SyntaxNode s)
        => s is StructDeclarationSyntax _struct &&
           _struct.AttributeLists.Any(x => x.Attributes.Any(a => a.Name.ToString() == "Packet"));

    private static PacketInformation? PacketTransform(GeneratorSyntaxContext ctx) {
        var _struct = (StructDeclarationSyntax)ctx.Node;
        var attr = _struct.AttributeLists
            .SelectMany(x => x.Attributes)
            .FirstOrDefault(a => a.Name.ToString() == "Packet");
        if(attr is null) {
            return null;
        }

        var methods = _struct.Members.OfType<MethodDeclarationSyntax>().ToList();

        var fields = _struct.Members.OfType<FieldDeclarationSyntax>()
            .Where(x => x.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)) &&
                        !x.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)))
            .SelectMany(x => x.Declaration.Variables.Select(t => new FieldInformation(
                x.Declaration.Type.ToString(),
                t.Identifier.Text,
                x.AttributeLists.Any(al => al.Attributes.Any(a => a.Name.ToString() == "TransferExplicit")))))
            .ToList();

        PacketInformation result = new() {
            Fields = fields,
            StructIdent = _struct.Identifier.Text,
            Partial = _struct.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)),
            Location = _struct.Identifier.GetLocation(),
            Clientbound = BoundParameterType(methods, "Clientbound"),
            Serverbound = BoundParameterType(methods, "Serverbound"),
        };

        if(attr.ArgumentList is AttributeArgumentListSyntax list && list.Arguments.Count >= 2) {
            result.Ordered = list.Arguments[0].Expression.IsKind(SyntaxKind.TrueLiteralExpression);
            result.Reliable = list.Arguments[1].Expression.IsKind(SyntaxKind.TrueLiteralExpression);
        }

        result.StructLine =
            $"{_struct.Modifiers.ToFullString()}" +
            $"{_struct.Keyword.ToFullString()}" +
            $"{result.StructIdent}{(_struct.TypeParameterList is null ? "" : _struct.TypeParameterList.ToString())} " +
            $"{(_struct.BaseList != null ? _struct.BaseList.ToFullString() : "")}" +
            $"{_struct.ConstraintClauses.ToFullString()}";

        (result.Namespace, result.Usings) = ResolveNamespaceAndUsings(_struct);
        return result;
    }

    private static string? BoundParameterType(IEnumerable<MethodDeclarationSyntax> methods, string name) {
        var method = methods.FirstOrDefault(x => x.Identifier.Text == name);
        var parameters = method?.ParameterList.Parameters;
        if(parameters is null || parameters.Value.Count == 0) {
            return null;
        }
        return parameters.Value[0].Type?.ToString();
    }

    private static bool IsNetworkCandidate(SyntaxNode s)
        => s is ClassDeclarationSyntax _class &&
           _class.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword)) &&
           _class.BaseList is not null;

    private static NetworkInformation? NetworkTransform(GeneratorSyntaxContext ctx) {
        var _class = (ClassDeclarationSyntax)ctx.Node;
        if(ctx.SemanticModel.GetDeclaredSymbol(_class) is not INamedTypeSymbol symbol) {
            return null;
        }

        NetworkKind? kind = null;
        for(var baseType = symbol.BaseType; baseType is not null; baseType = baseType.BaseType) {
            if(baseType.ContainingNamespace?.ToDisplayString() != "NanoPackets") {
                continue;
            }
            if(baseType.Name == "NetworkServerBase") { kind = NetworkKind.Server; break; }
            if(baseType.Name == "NetworkClientBase") { kind = NetworkKind.Client; break; }
        }
        if(kind is null) {
            return null;
        }

        NetworkInformation result = new() {
            Location = _class.GetLocation(),
            ClassIdent = _class.Identifier.Text,
            Kind = kind.Value,
        };

        result.ClassLine =
            $"{_class.Modifiers.ToFullString()}" +
            $"{_class.Keyword.ToFullString()}" +
            $"{result.ClassIdent}{(_class.TypeParameterList is null ? "" : _class.TypeParameterList.ToString())} " +
            $"{(_class.BaseList != null ? _class.BaseList.ToFullString() : "")}" +
            $"{_class.ConstraintClauses.ToFullString()}";

        (result.Namespace, result.Usings) = ResolveNamespaceAndUsings(_class);
        return result;
    }

    private static (string Namespace, string Usings) ResolveNamespaceAndUsings(SyntaxNode node) {
        string @namespace = string.Empty;
        var usings = new StringBuilder();

        var parent = node.Parent;
        while(parent is not null and not BaseNamespaceDeclarationSyntax) {
            if(parent.Parent is null) {
                break;
            }
            parent = parent.Parent;
        }

        if(parent is BaseNamespaceDeclarationSyntax _namespace) {
            @namespace = _namespace.Name.ToString();
            if(_namespace.Parent is not null) {
                AppendUsings(usings, _namespace.Parent.ChildNodes());
            }
        } else if(parent is not null) {
            AppendUsings(usings, parent.ChildNodes());
        }

        return (@namespace, usings.ToString());
    }

    private static void AppendUsings(StringBuilder sb, IEnumerable<SyntaxNode> nodes) {
        foreach(var child in nodes) {
            if(child is UsingDirectiveSyntax _using) {
                sb.Append(_using.ToString());
                sb.Append('\n');
            }
        }
    }

    private static ExtensionInformation CollectExtensions(Compilation compilation) {
        var usings = new StringBuilder();
        var typeNameExtensions = new Dictionary<string, string>();

        var assemblies = compilation.References
            .Where(r => r.Display?.Contains("NanoPackets") ?? false)
            .Select(compilation.GetAssemblyOrModuleSymbol)
            .OfType<IAssemblySymbol>()
            .OrderBy(a => a.Name, StringComparer.Ordinal);

        foreach(var assembly in assemblies) {
            CollectExtensionsFromNamespace(assembly.GlobalNamespace, usings, typeNameExtensions);
        }

        return new ExtensionInformation {
            Usings = usings.ToString(),
            TypeNameExtensions = typeNameExtensions,
        };
    }

    private static void CollectExtensionsFromNamespace(
        INamespaceSymbol ns, StringBuilder usings, Dictionary<string, string> typeNameExtensions) {
        foreach(var type in ns.GetTypeMembers().Where(t => t.Name == "Extensions")) {
            var methods = type.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m.Parameters.Length == 2 && m.Name != "Add" && !m.Parameters[1].Type.Name.EndsWith("[]"))
                .OrderBy(m => m.Name, StringComparer.Ordinal);
            foreach(var method in methods) {
                var key = method.Parameters[1].Type.Name;
                if(!typeNameExtensions.ContainsKey(key)) {
                    typeNameExtensions.Add(key, method.Name.Substring(3));
                }
            }
            usings.Append($"using {type.ContainingNamespace.ToDisplayString()};\n");
        }

        foreach(var child in ns.GetNamespaceMembers().OrderBy(n => n.Name, StringComparer.Ordinal)) {
            CollectExtensionsFromNamespace(child, usings, typeNameExtensions);
        }
    }

    // ---- Emission ---------------------------------------------------------------------------

    private static void Emit(
        SourceProductionContext context,
        ImmutableArray<PacketInformation> packets,
        ImmutableArray<NetworkInformation> networks,
        ExtensionInformation extensions) {

        var servers = networks.Where(n => n.Kind == NetworkKind.Server).ToList();
        var clients = networks.Where(n => n.Kind == NetworkKind.Client).ToList();

        ReportDuplicates(context, servers, MultipleServers);
        ReportDuplicates(context, clients, MultipleClients);

        // Validate user packets; emit diagnostics for unusable ones and drop them.
        var validPackets = new List<PacketInformation>();
        foreach(var packet in packets) {
            if(!packet.Partial) {
                context.ReportDiagnostic(Diagnostic.Create(PacketNotPartial, packet.Location, packet.StructIdent));
                continue;
            }
            if(packet.Fields.Count == 0) {
                context.ReportDiagnostic(Diagnostic.Create(PacketWithoutFields, packet.Location, packet.StructIdent));
                continue;
            }
            validPackets.Add(packet);
        }

        // Deterministic ordering: packet identity (and therefore its wire id in the enum) must be
        // stable across builds and across separately-compiled client/server assemblies.
        validPackets.Sort((a, b) => string.CompareOrdinal(a.StructIdent, b.StructIdent));

        var internalPackets = typeof(MainGenerator).Assembly
            .GetManifestResourceNames()
            .Where(x => x.Contains("NanoPackets.Generator.Packets"))
            .Select(x => x.Split('.')[^2])
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        var serverHandlers = new List<string>();
        var clientHandlers = new List<string>();
        var packetNamespaces = new SortedSet<string>(StringComparer.Ordinal);

        // Per-user-packet emission + handler/enum metadata.
        var enumEntries = new List<string>();
        foreach(var packet in validPackets) {
            var id = packet.StructIdent.EndsWith("Packet") ? packet.StructIdent[..^6] : packet.StructIdent;
            if(!string.IsNullOrWhiteSpace(packet.Namespace)) {
                packetNamespaces.Add(packet.Namespace);
            }
            AddHandler(serverHandlers, clientHandlers, id, packet.StructIdent,
                clientbound: packet.Clientbound != null, serverbound: packet.Serverbound != null);
            enumEntries.Add(id);
            context.AddSource(
                $"{(string.IsNullOrWhiteSpace(packet.Namespace) ? "" : $"{packet.Namespace}.")}{packet.StructIdent}.g.cs",
                BuildPacketSource(packet, extensions));
        }

        packetNamespaces.Add("NanoPackets.Packets");

        var hasServer = servers.Count > 0;
        var hasClient = clients.Count > 0;
        if(!hasServer) {
            context.ReportDiagnostic(Diagnostic.Create(MissingServer, null));
        }
        if(!hasClient) {
            context.ReportDiagnostic(Diagnostic.Create(MissingClient, null));
        }

        // Internal packets need the concrete server/client type names; only generate them (and the
        // PacketId enum entries that reference them) when both are present.
        if(hasServer && hasClient) {
            var server = servers[0];
            var client = clients[0];

            var usingString = new StringBuilder(extensions.Usings);
            if(!string.IsNullOrWhiteSpace(client.Namespace)) {
                usingString.Append($"using {client.Namespace};\n");
            }
            if(!string.IsNullOrWhiteSpace(server.Namespace) && server.Namespace != client.Namespace) {
                usingString.Append($"using {server.Namespace};\n");
            }

            foreach(var packet in internalPackets) {
                var template = ReadManifestString($"NanoPackets.Generator.Packets.{packet}.cs")
                    .Replace("NetworkClient", client.ClassIdent)
                    .Replace("NetworkServer", server.ClassIdent);
                var result = usingString + template;
                AddHandler(serverHandlers, clientHandlers, packet[..^6], packet,
                    clientbound: result.Contains("Clientbound("), serverbound: result.Contains("Serverbound("));
                enumEntries.Add(packet[..^6]);
                context.AddSource($"NanoPackets.Packets.{packet}.g.cs", result);
            }
        } else {
            // Still surface the internal packets in the enum so downstream references resolve.
            foreach(var packet in internalPackets) {
                enumEntries.Add(packet[..^6]);
            }
        }

        context.AddSource("NanoPackets.PacketId.g.cs", BuildEnum(enumEntries));

        if(hasServer && hasClient) {
            EmitServerPartials(context, servers[0], serverHandlers, packetNamespaces);
            EmitClientPartials(context, clients[0], clientHandlers, packetNamespaces);
        }
    }

    private static void ReportDuplicates(
        SourceProductionContext context, List<NetworkInformation> networks, DiagnosticDescriptor descriptor) {
        if(networks.Select(n => n.ClassIdent).Distinct().Count() <= 1) {
            return;
        }
        foreach(var network in networks) {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, network.Location));
        }
    }

    private static void AddHandler(
        List<string> serverHandlers, List<string> clientHandlers,
        string packetId, string packet, bool clientbound, bool serverbound) {
        var line = $"PacketId.{packetId} => {packet}.Read,";
        if(clientbound) {
            clientHandlers.Add(line);
        }
        if(serverbound) {
            serverHandlers.Add(line);
        }
    }

    private static string BuildEnum(IEnumerable<string> entries) {
        var source = new StringBuilder();
        source.AppendLine("namespace NanoPackets;");
        source.AppendLine("public enum PacketId {");
        foreach(var entry in entries) {
            source.AppendLine($"    {entry},");
        }
        source.AppendLine("    Unknown");
        source.AppendLine("}");
        return source.ToString();
    }

    private static void EmitServerPartials(
        SourceProductionContext context, NetworkInformation server,
        List<string> serverHandlers, IEnumerable<string> packetNamespaces) {

        var (usings, classLines) = BuildPartialPreamble(server, packetNamespaces);

        var broadcast = ReadManifestString("NanoPackets.Generator.Templates.Broadcast.cs")
            .Split(new[] { "/* CLASS_LINE */" }, StringSplitOptions.None);
        usings += broadcast[0];
        context.AddSource(
            $"{(string.IsNullOrWhiteSpace(server.Namespace) ? "" : $"{server.Namespace}.")}{server.ClassIdent}Broadcast.g.cs",
            usings + classLines + broadcast[1]);

        var handlers = ReadManifestString("NanoPackets.Generator.Templates.ServerHandlers.cs")
            .Split(new[] { "/* CLASS_LINE */" }, StringSplitOptions.None);
        usings += handlers[0];
        var result = usings + classLines + handlers[1]
            .Replace("NetworkServer", server.ClassIdent)
            .Replace("/* PACKET_HANDLERS */", string.Join("\n            ", serverHandlers));
        context.AddSource(
            $"{(string.IsNullOrWhiteSpace(server.Namespace) ? "" : $"{server.Namespace}.")}{server.ClassIdent}Handlers.g.cs",
            result);
    }

    private static void EmitClientPartials(
        SourceProductionContext context, NetworkInformation client,
        List<string> clientHandlers, IEnumerable<string> packetNamespaces) {

        var (usings, classLines) = BuildPartialPreamble(client, packetNamespaces);

        var handlers = ReadManifestString("NanoPackets.Generator.Templates.ClientHandlers.cs")
            .Split(new[] { "/* CLASS_LINE */" }, StringSplitOptions.None);
        usings += handlers[0];
        var result = usings + classLines + handlers[1]
            .Replace("NetworkClient", client.ClassIdent)
            .Replace("/* PACKET_HANDLERS */", string.Join("\n            ", clientHandlers));
        context.AddSource(
            $"{(string.IsNullOrWhiteSpace(client.Namespace) ? "" : $"{client.Namespace}.")}{client.ClassIdent}Handlers.g.cs",
            result);
    }

    private static (string Usings, string ClassLines) BuildPartialPreamble(
        NetworkInformation network, IEnumerable<string> packetNamespaces) {
        var usings = network.Usings;
        string classLines;
        if(!string.IsNullOrWhiteSpace(network.Namespace)) {
            usings += $"using {network.Namespace};\n";
            classLines = $"\nnamespace {network.Namespace};\n{network.ClassLine}";
        } else {
            classLines = $"\n{network.ClassLine}";
        }

        foreach(var ns in packetNamespaces) {
            if(!usings.Contains(ns)) {
                usings += $"using {ns};\n";
            }
        }

        return (usings, classLines);
    }

    private static string BuildPacketSource(PacketInformation info, ExtensionInformation extensions) {
        var source = new StringBuilder();
        var hasNamespace = !string.IsNullOrWhiteSpace(info.Namespace);

        source.AppendLine("using Riptide;");
        if(!string.IsNullOrEmpty(extensions.Usings)) {
            source.AppendLine(extensions.Usings.TrimEnd('\n'));
        }
        source.AppendLine("using System.Runtime.InteropServices;");
        source.AppendLine("using System.Runtime.CompilerServices;");
        source.AppendLine(info.Usings);
        if(hasNamespace) {
            source.AppendLine($"namespace {info.Namespace};");
            source.AppendLine();
        }
        source.AppendLine("[StructLayout(LayoutKind.Auto)]");
        source.AppendLine($"{info.StructLine}{{");

        var dynamicSendMode = info.Ordered == null;
        if(dynamicSendMode) {
            source.AppendLine("    readonly bool ordered;");
            source.AppendLine("    readonly bool reliable;");
        }

        var parameters = string.Join(", ", info.Fields.Select(x => $"{x.Type} {x.Name.ToCamelCase()}"));
        source.AppendLine($"    public {info.StructIdent}({(dynamicSendMode ? "bool ordered, bool reliable, " : "")}{parameters}) {{");
        if(dynamicSendMode) {
            source.AppendLine("        this.ordered = ordered;");
            source.AppendLine("        this.reliable = reliable;");
        }
        foreach(var field in info.Fields) {
            source.AppendLine($"        this.{field.Name} = {field.Name.ToCamelCase()};");
        }
        source.AppendLine("    }");
        if(dynamicSendMode) {
            source.AppendLine();
            source.AppendLine($"    {info.StructIdent}({parameters}) {{");
            foreach(var field in info.Fields) {
                source.AppendLine($"        this.{field.Name} = {field.Name.ToCamelCase()};");
            }
            source.AppendLine("    }");
        }
        source.AppendLine();

        var sendMode = dynamicSendMode
            ? "sendMode"
            : (info.Ordered == true ? "MessageSendMode.Notify" : (info.Reliable == true ? "MessageSendMode.Reliable" : "MessageSendMode.Unreliable"));
        source.AppendLine("    public Message Write() {");
        if(dynamicSendMode) {
            source.AppendLine("        MessageSendMode sendMode;");
            source.AppendLine("        if(ordered) {");
            source.AppendLine("            sendMode = MessageSendMode.Notify;");
            source.AppendLine("        } else if(reliable) {");
            source.AppendLine("            sendMode = MessageSendMode.Reliable;");
            source.AppendLine("        } else {");
            source.AppendLine("            sendMode = MessageSendMode.Unreliable;");
            source.AppendLine("        }");
            source.AppendLine();
        }
        source.AppendLine($"        var msg = Message.Create({sendMode}, PacketId.Batch);");
        if(dynamicSendMode) {
            source.AppendLine("        if(ordered) {");
            source.AppendLine("            msg.AddBool(reliable);");
            source.AppendLine("        }");
        }
        source.AppendLine();
        foreach(var field in info.Fields) {
            source.AppendLine($"        msg.Add{IntoTypeName(field.Type, field.IsExplicit, extensions.TypeNameExtensions)}({field.Name});");
        }
        source.AppendLine();
        source.AppendLine("        return msg;");
        source.AppendLine("    }");
        source.AppendLine();
        source.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        source.AppendLine($"    public static {info.StructIdent} Read(Message _msg) {{");
        if(dynamicSendMode) {
            source.AppendLine("        if(_msg.SendMode == MessageSendMode.Notify) {");
            source.AppendLine("            _msg.GetBool();");
            source.AppendLine("        }");
            source.AppendLine();
        }
        foreach(var field in info.Fields) {
            source.AppendLine($"        {field.Type} {field.Name.ToCamelCase()} = _msg.Get{IntoTypeName(field.Type, field.IsExplicit, extensions.TypeNameExtensions)}();");
        }
        source.AppendLine();
        source.AppendLine($"        return new {info.StructIdent}({string.Join(", ", info.Fields.Select(x => x.Name.ToCamelCase()))});");
        source.AppendLine("    }");

        if(info.Clientbound is string client) {
            source.AppendLine($"    public static void Read(Message _msg, {client} _network, int _player) => Read(_msg).Clientbound(_network, _player);");
        }
        if(info.Serverbound is string server) {
            source.AppendLine($"    public static void Read(Message _msg, {server} _network, ushort _player) => Read(_msg).Serverbound(_network, _player);");
        }

        source.AppendLine("}");
        return source.ToString();
    }

    // ---- Resources & type mapping -----------------------------------------------------------

    private static string ReadManifestString(string name) {
        var assembly = typeof(MainGenerator).Assembly;
        using var stream = assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded resource '{name}' was not found in the generator assembly.");
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private static string IntoTypeName(string type, bool isExplicit, Dictionary<string, string> typeNameExtensions) {
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
                _ => IntoGetBase(type, ref isArray, typeNameExtensions)
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
                    result = IntoGetBase(type, ref isArray, typeNameExtensions);
                    break;
            }
        }
        if(isArray) {
            result += 's';
        }
        return result;
    }

    private static string IntoGetBase(string type, ref bool isArray, Dictionary<string, string> typeNameExtensions) {
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
        return result!;
    }
}
