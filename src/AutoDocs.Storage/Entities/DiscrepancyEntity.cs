using AutoDocs.Core.Model;

namespace AutoDocs.Storage.Entities;

public class DiscrepancyEntity
{
    public int Id { get; set; }

    public int CheckRunId { get; set; }

    public CheckRunEntity CheckRun { get; set; } = null!;

    public DiscrepancyKind Kind { get; set; }

    public DiscrepancySeverity Severity { get; set; }

    public string EndpointKey { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string? FilePath { get; set; }

    public int? Line { get; set; }
}
