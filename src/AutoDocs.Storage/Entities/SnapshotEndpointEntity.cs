using AutoDocs.Core.Model;

namespace AutoDocs.Storage.Entities;

public class SnapshotEndpointEntity
{
    public int Id { get; set; }

    public int ApiSnapshotId { get; set; }

    public ApiSnapshotEntity ApiSnapshot { get; set; } = null!;

    public HttpVerb HttpMethod { get; set; }

    public string Route { get; set; } = string.Empty;

    public string EndpointKey { get; set; } = string.Empty;

    public string? Summary { get; set; }

    public string? Returns { get; set; }

    public string ParametersJson { get; set; } = "[]";

    public string ResponsesJson { get; set; } = "[]";

    public string? FilePath { get; set; }

    public int? Line { get; set; }
}
