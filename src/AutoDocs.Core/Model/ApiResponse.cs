

namespace AutoDocs.Core.Model
{
    public record ApiResponse
    (
        int StatusCode,
        string? TypeName = null
    );
}
