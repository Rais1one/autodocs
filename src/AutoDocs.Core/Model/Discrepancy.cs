namespace AutoDocs.Core.Model;

public record Discrepancy(
    DiscrepancyKind Kind,
    DiscrepancySeverity Severity,
    string EndpointKey,
    string Message,
    string? OldValue = null,
    string? NewValue = null,
    string? FilePath = null,
    int? Line = null
);