using AutoDocs.Core.Generation;

namespace AutoDocs.Core.Normalization;

public static class TypeNormalizer
{
    private static readonly string[] CollectionTypes =
    {
        "IEnumerable",
        "ICollection",
        "IList",
        "List",
        "IReadOnlyList",
        "IReadOnlyCollection",
        "HashSet",
        "Collection",
        "IAsyncEnumerable"
    };

    public static string Normalize(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return string.Empty;

        typeName = typeName.Trim().TrimEnd('?');

        var (type, format) = TypeMapper.Map(typeName);

        
        if (type != "object")
        {
            return string.IsNullOrEmpty(format)
                ? type
                : $"{type}:{format}";
        }

        
        if (typeName.EndsWith("[]"))
        {
            var itemType = typeName[..^2];

            return $"array<{Normalize(itemType)}>";
        }

        
        foreach (var collection in CollectionTypes)
        {
            var prefix = collection + "<";

            if (typeName.StartsWith(prefix) &&
                typeName.EndsWith(">"))
            {
                var itemType = typeName[prefix.Length..^1];

                return $"array<{Normalize(itemType)}>";
            }
        }

        
        return typeName;
    }

    public static bool AreEquivalent(
        string? first,
        string? second)
    {
        return string.Equals(
            Normalize(first),
            Normalize(second),
            StringComparison.Ordinal);
    }
}