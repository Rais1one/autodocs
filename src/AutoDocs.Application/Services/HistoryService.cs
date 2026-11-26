using AutoDocs.Application.Interfaces;
using AutoDocs.Application.Models;
using AutoDocs.Core.Model;

namespace AutoDocs.Application.Services;

public class HistoryService
{
    private readonly IHistoryRepository _repository;

    public HistoryService(IHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task SaveCheckAsync(
        CheckResult result,
        string source)
    {
        if (source is not ("CLI" or "WPF"))
        {
            throw new ArgumentException(
                "Неизвестный источник проверки.",
                nameof(source));
        }

        await _repository.SaveCheckAsync(result, source);
    }

    public Task<IReadOnlyList<CheckHistoryItem>> GetHistoryAsync()
    {
        return _repository.GetHistoryAsync();
    }

    public Task<IReadOnlyList<Discrepancy>> GetDiscrepanciesAsync(
        int checkRunId)
    {
        return _repository.GetDiscrepanciesAsync(checkRunId);
    }

    public Task SaveSnapshotAsync(
        string projectPath,
        GenerationResult result)
    {
        return _repository.SaveSnapshotAsync(projectPath, result);
    }
}
