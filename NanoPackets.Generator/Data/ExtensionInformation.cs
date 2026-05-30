using System.Collections.Generic;

namespace NanoPackets.Generator.Data;

/// <summary>
/// Custom serializer extension methods discovered in referenced assemblies. Collected once per
/// compilation and threaded through the pipeline so emission has a complete, consistent view
/// (rather than relying on static state populated by a separate pipeline step).
/// </summary>
public struct ExtensionInformation {
    /// <summary>Accumulated <c>using</c> directives for the namespaces that declare the discovered
    /// <c>Extensions</c> classes (newline-terminated, may be empty).</summary>
    public string Usings;

    /// <summary>Maps a serialized type's simple name to the suffix used for its
    /// <c>Add{suffix}</c>/<c>Get{suffix}</c> methods.</summary>
    public Dictionary<string, string> TypeNameExtensions;
}
