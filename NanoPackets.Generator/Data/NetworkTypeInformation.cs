namespace NanoPackets.Generator.Data;

public struct NetworkTypeInformation {
    public string Name;
    public string Namespace;
    public NetworkTypeInformation(string name, string @namespace) {
        Name = name;
        Namespace = @namespace;
    }
}
