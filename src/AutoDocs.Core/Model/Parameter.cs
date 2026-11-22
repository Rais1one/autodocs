

namespace AutoDocs.Core.Model
{
    public record Parameter
    (
        string Name,
        string Type,
        ParameterSource Source,
        bool IsRequired
    );
}
