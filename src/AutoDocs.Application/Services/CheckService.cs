using System.Diagnostics;
using AutoDocs.Application.Models;
using AutoDocs.Core.Analysis;
using AutoDocs.Core.Comparison;

namespace AutoDocs.Application.Services;

public class CheckService
{
    private readonly ProjectAnalyzer _analyzer;
    private readonly OpenApiReader _reader;
    private readonly ApiComparer _comparer;
    private readonly HistoryService? _historyService;

    public CheckService(
    ProjectAnalyzer analyzer,
    OpenApiReader reader,
    ApiComparer comparer,
    HistoryService? historyService = null)
    {
        _analyzer = analyzer;
        _reader = reader;
        _comparer = comparer;
        _historyService = historyService;
    }


    public async Task<CheckResult> CheckAsync(
        CheckRequest request)
    {
        var stopwatch = Stopwatch.StartNew();

        var projectPath = Path.GetFullPath(request.ProjectPath);

        if (!File.Exists(projectPath))
        {
            throw new FileNotFoundException(
                "Файл проекта не найден.",
                projectPath);
        }

        if (!string.Equals(
                Path.GetExtension(projectPath),
                ".csproj",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Необходимо указать файл .csproj.");
        }

        var documentationPath =
            string.IsNullOrWhiteSpace(request.DocumentationPath)
                ? Path.Combine(
                    Path.GetDirectoryName(projectPath)!,
                    "docs",
                    "openapi.json")
                : Path.GetFullPath(request.DocumentationPath);

        if (!File.Exists(documentationPath))
        {
            throw new FileNotFoundException(
                "Файл документации не найден.",
                documentationPath);
        }

        var documented = await _reader.ReadAsync(documentationPath);

        var current = await _analyzer.AnalyzeAsync(projectPath);

        var discrepancies = _comparer.Compare(documented, current);

        stopwatch.Stop();

        var result = new CheckResult(
            current,
            documented,
            discrepancies.ToList(),
            projectPath,
            documentationPath,
            stopwatch.Elapsed);

        if (request.SaveHistory && _historyService is not null)
        {
            try
            {
                await _historyService.SaveCheckAsync(
                    result,
                    request.Source);
            }
            catch (Exception ex)
            {
                result = result with
                {
                    HistoryWarning =
                        $"Не удалось сохранить историю: {ex.Message}"
                };
            }
        }

        return result;

    }
}

