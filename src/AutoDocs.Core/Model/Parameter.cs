namespace AutoDocs.Core.Model;

public record Parameter(
    string Name,
    string Type,
    ParameterSource Source,
    bool IsRequired,
    string? Description = null
)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public Microsoft.OpenApi.IOpenApiSchema? Schema { get; init; }
}
