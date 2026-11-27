namespace AutoDocs.Core.Generation;

public static class TypeMapper
{
    private static readonly Dictionary<string, (string Type, string? Format)>
        PrimitiveTypesAndFormats = new()
        {
            ["Boolean"] = ("boolean", null),
            ["Byte"] = ("integer", "int32"),
            ["SByte"] = ("integer", "int32"),
            ["Int16"] = ("integer", "int32"),
            ["UInt16"] = ("integer", "int32"),
            ["Int32"] = ("integer", "int32"),
            ["UInt32"] = ("integer", "int32"),
            ["Int64"] = ("integer", "int64"),
            ["UInt64"] = ("integer", "int64"),
            ["Single"] = ("number", "float"),
            ["Double"] = ("number", "double"),
            ["Decimal"] = ("number", "double"),
            ["Byte[]"] = ("string", "byte"),
            ["String"] = ("string", null),
            ["Char"] = ("string", null),
            ["DateTime"] = ("string", "date-time"),
            ["DateTimeOffset"] = ("string", "date-time"),
            ["TimeSpan"] = ("string", "date-span"),
            ["Guid"] = ("string", "uuid"),
            ["Uri"] = ("string", "uri"),
            ["Version"] = ("string", null),
            ["DateOnly"] = ("string", "date"),
            ["TimeOnly"] = ("string", "time"),
            ["Int128"] = ("integer", "int128"),
            ["UInt128"] = ("integer", "int128")
        };

    public static (string Type, string? Format) Map(string typeName)
    {
        typeName = typeName.Trim().TrimEnd('?');

        typeName = typeName
            .Replace("global::", "")
            .Replace("System.", "");

        typeName = typeName switch
        {
            "bool" => "Boolean",
            "byte" => "Byte",
            "sbyte" => "SByte",
            "short" => "Int16",
            "ushort" => "UInt16",
            "int" => "Int32",
            "uint" => "UInt32",
            "long" => "Int64",
            "ulong" => "UInt64",
            "float" => "Single",
            "double" => "Double",
            "decimal" => "Decimal",
            "string" => "String",
            "char" => "Char",
            "byte[]" => "Byte[]",
            _ => typeName
        };

        return PrimitiveTypesAndFormats.TryGetValue(
            typeName, out var result)
            ? result
            : ("object", null);
    }
}