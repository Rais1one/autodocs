namespace AutoDocs.Application.Models;

public record CheckHistoryItem(
    int Id,
    string ProjectName,
    string ProjectPath,
    string DocumentationPath,
    DateTime CheckedAt,
    string Source,
    int ExitCode,
    int ErrorCount,
    int WarningCount,
    int InfoCount,
    int CurrentEndpointCount,
    int DocumentedEndpointCount,
    long DurationMs
);
