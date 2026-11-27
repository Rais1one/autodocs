using System.Text.Json;
using AutoDocs.Core.Generation;
using Microsoft.CodeAnalysis;
using Microsoft.OpenApi;

namespace AutoDocs.Core.Analysis;

internal sealed class DtoSchemas
{
    private readonly OpenApiDocument document = new()
    {
        Components = new OpenApiComponents { Schemas = new Dictionary<string, IOpenApiSchema>() }
    };

    internal OpenApiComponents Components => document.Components!;

    internal IOpenApiSchema Create(ITypeSymbol symbol)
    {
        if (symbol is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
            return Create(nullable.TypeArguments[0]);

        string name = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat).TrimEnd('?');
        var basic = OpenApiGenerator.CreateSchema(name);
        if (basic.Type != JsonSchemaType.Object && basic.Type != JsonSchemaType.Array)
            return basic;
        if (symbol is IArrayTypeSymbol array)
            return new OpenApiSchema { Type = JsonSchemaType.Array, Items = Create(array.ElementType) };
        if (symbol is not INamedTypeSymbol type)
            return basic;
        if (basic.Type == JsonSchemaType.Array && type.TypeArguments.Length == 1)
            return new OpenApiSchema { Type = JsonSchemaType.Array, Items = Create(type.TypeArguments[0]) };
        if (!type.Locations.Any(location => location.IsInSource))
            return basic;

        var id = Uri.EscapeDataString(type.ToDisplayString()).Replace("%", "_");
        var definitions = Components.Schemas!;
        if (!definitions.ContainsKey(id))
        {
            var schema = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Title = name,
                Properties = new Dictionary<string, IOpenApiSchema>(),
                Required = new SortedSet<string>(StringComparer.Ordinal)
            };
            definitions.Add(id, schema);
            for (var owner = type; owner is not null; owner = owner.BaseType)
            {
                foreach (var property in owner.GetMembers().OfType<IPropertySymbol>())
                {
                    if (property.IsStatic || property.IsIndexer || property.GetMethod?.DeclaredAccessibility != Accessibility.Public)
                        continue;
                    var attributes = property.GetAttributes();
                    if (attributes.Any(a => a.AttributeClass?.ToDisplayString() == "System.Text.Json.Serialization.JsonIgnoreAttribute" && a.NamedArguments.Length == 0))
                        continue;
                    var alias = attributes.FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "System.Text.Json.Serialization.JsonPropertyNameAttribute");
                    var key = alias?.ConstructorArguments.FirstOrDefault().Value as string ?? JsonNamingPolicy.CamelCase.ConvertName(property.Name);
                    if (schema.Properties.ContainsKey(key)) continue;
                    var propertySchema = Create(property.Type);
                    if (property.NullableAnnotation == NullableAnnotation.Annotated && propertySchema is OpenApiSchema inline)
                        inline.Type |= JsonSchemaType.Null;
                    schema.Properties.Add(key, propertySchema);
                    if (property.IsRequired || attributes.Any(a => a.AttributeClass?.Name is "RequiredAttribute" or "JsonRequiredAttribute")
                        || property.NullableAnnotation != NullableAnnotation.Annotated && property.Type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
                        schema.Required.Add(key);
                }
            }
        }
        document.RegisterComponents();
        return new OpenApiSchemaReference(id, document);
    }
}
