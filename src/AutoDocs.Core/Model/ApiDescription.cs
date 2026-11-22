

namespace AutoDocs.Core.Model
{
    public record ApiDescription
    (
        string Title,
        string Version,
        IReadOnlyList<Endpoint> Endpoints 

    );
}
