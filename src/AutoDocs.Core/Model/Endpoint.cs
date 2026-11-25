namespace AutoDocs.Core.Model;

public record Endpoint(
    HttpVerb HttpMethod,
    string Route,
    string Key,
    IReadOnlyList<Parameter> Parameters,
    IReadOnlyList<ApiResponse> Responses,
    string? Summary = null,
    string? Returns = null,
    string? FilePath = null,
    int? Line = null
);