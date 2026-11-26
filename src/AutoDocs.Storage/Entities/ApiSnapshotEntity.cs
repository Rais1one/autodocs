namespace AutoDocs.Storage.Entities;

public class ApiSnapshotEntity
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public ProjectEntity Project { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string? OpenApiPath { get; set; }

    public string? MarkdownPath { get; set; }

    public ICollection<SnapshotEndpointEntity> Endpoints { get; }
        = new List<SnapshotEndpointEntity>();
}
