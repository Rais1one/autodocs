using System.Text.Json.Serialization;
using Microsoft.OpenApi;

namespace AutoDocs.Core.Model;

public record ApiResponse(int StatusCode, string? TypeName = null)
{
    [JsonIgnore]
    public IOpenApiSchema? Schema { get; init; }
}
