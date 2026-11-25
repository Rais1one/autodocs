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

    public CheckService(
        ProjectAnalyzer analyzer,
        OpenApiReader reader,
        ApiComparer comparer)
    {
        _analyzer = analyzer;
        _reader = reader;
        _comparer = comparer;
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

        return new CheckResult(
            current,
            documented,
            discrepancies.ToList(),
            projectPath,
            documentationPath,
            stopwatch.Elapsed);
    }
}

