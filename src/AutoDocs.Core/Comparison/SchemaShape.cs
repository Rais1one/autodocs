using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace AutoDocs.Core.Comparison;

internal static class SchemaShape
{

    internal static string? Describe(IOpenApiSchema? schema)
    {
        if (schema is null) return null;
        var element = schema;
        var visited = new HashSet<IOpenApiSchema>(ReferenceEqualityComparer.Instance);
        while (element.Type == JsonSchemaType.Array && element.Items is not null)
        {
            if (!visited.Add(element)) return null;
            element = element.Items;
        }
        if (element.Properties is not { Count: > 0 }) return null;
        return DescribeNode(schema, new HashSet<string>()).ToJsonString();
    }

    private static JsonObject DescribeNode(IOpenApiSchema schema, HashSet<string> ancestors)
    {
        var reference = (schema as OpenApiSchemaReference)?.Reference.Id;
        if (reference is not null && !ancestors.Add(reference))
            return new JsonObject { ["recursive"] = schema.Title ?? reference };

        var node = new JsonObject { ["type"] = schema.Type?.ToString() };
        if (schema.Format is not null) node["format"] = schema.Format;
        if (schema.Items is not null) node["items"] = DescribeNode(schema.Items, ancestors);
        if (schema.Properties is { Count: > 0 })
        {
            var properties = new JsonObject();
            foreach (var (key, property) in schema.Properties.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                var child = DescribeNode(property, ancestors);
                child["required"] = schema.Required?.Contains(key) == true;
                properties[key] = child;
            }
            node["properties"] = properties;
        }
        if (reference is not null) ancestors.Remove(reference);
        return node;
    }
}
