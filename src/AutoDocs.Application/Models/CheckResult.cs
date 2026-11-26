using AutoDocs.Core.Model;

namespace AutoDocs.Application.Models;

public record CheckResult(
    ApiDescription Current,
    ApiDescription Documented,
    IReadOnlyList<Discrepancy> Discrepancies,
    string ProjectPath,
    string DocumentationPath,
    TimeSpan Duration
)
{
    public string? HistoryWarning { get; init; }

    public bool IsUpToDate => Discrepancies.Count == 0;

    public int ErrorCount => Discrepancies.Count(
        d => d.Severity == DiscrepancySeverity.Error);

    public int WarningCount => Discrepancies.Count(
        d => d.Severity == DiscrepancySeverity.Warning);

    public int InfoCount => Discrepancies.Count(
        d => d.Severity == DiscrepancySeverity.Info);
}
