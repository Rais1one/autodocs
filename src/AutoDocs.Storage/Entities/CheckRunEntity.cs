namespace AutoDocs.Storage.Entities;

public class CheckRunEntity
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public ProjectEntity Project { get; set; } = null!;

    public DateTime CheckedAt { get; set; }

    public string DocumentationPath { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public int ExitCode { get; set; }

    public int ErrorCount { get; set; }

    public int WarningCount { get; set; }

    public int InfoCount { get; set; }

    public int CurrentEndpointCount { get; set; }

    public int DocumentedEndpointCount { get; set; }

    public long DurationMs { get; set; }

    public ICollection<DiscrepancyEntity> Discrepancies { get; }
        = new List<DiscrepancyEntity>();
}
