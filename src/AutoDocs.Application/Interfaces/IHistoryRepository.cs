using AutoDocs.Application.Models;
using AutoDocs.Core.Model;

namespace AutoDocs.Application.Interfaces;

public interface IHistoryRepository
{
    Task SaveCheckAsync(
        CheckResult result,
        string source);

    Task<IReadOnlyList<CheckHistoryItem>> GetHistoryAsync();

    Task<IReadOnlyList<Discrepancy>> GetDiscrepanciesAsync(
        int checkRunId);

    Task SaveSnapshotAsync(
        string projectPath,
        GenerationResult result);
}
