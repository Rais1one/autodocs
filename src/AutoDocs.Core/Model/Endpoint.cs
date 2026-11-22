


namespace AutoDocs.Core.Model
{
    public record Endpoint
    (
        HttpVerb HttpMethod,
        string Route,
        string Key,
        IReadOnlyList<Parameter> Parameters,
        IReadOnlyList<ApiResponse> Responses

    );
    
}
