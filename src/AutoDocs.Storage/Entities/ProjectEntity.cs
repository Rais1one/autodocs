namespace AutoDocs.Storage.Entities;

public class ProjectEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ProjectPath { get; set; } = string.Empty;

    public ICollection<CheckRunEntity> CheckRuns { get; }
        = new List<CheckRunEntity>();

    public ICollection<ApiSnapshotEntity> ApiSnapshots { get; }
        = new List<ApiSnapshotEntity>();
}
