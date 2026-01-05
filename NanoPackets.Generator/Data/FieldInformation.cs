namespace NanoPackets.Generator.Data;

public struct FieldInformation {
    public string Type;
    public string Name;
    public bool IsExplicit;
    public FieldInformation(string type, string name, bool isExplicit) {
        Type = type;
        Name = name;
        IsExplicit = isExplicit;
    }
}