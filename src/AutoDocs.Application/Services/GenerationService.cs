using System.Diagnostics;
using AutoDocs.Application.Models;
using AutoDocs.Core.Analysis;
using AutoDocs.Core.Generation;

namespace AutoDocs.Application.Services;

public class GenerationService
{
    private readonly ProjectAnalyzer _analyzer;
    private readonly OpenApiGenerator _openApiGenerator;
    private readonly MarkdownGenerator _markdownGenerator;

    public GenerationService(
        ProjectAnalyzer analyzer,
        OpenApiGenerator openApiGenerator,
        MarkdownGenerator markdownGenerator)
    {
        _analyzer = analyzer;
        _openApiGenerator = openApiGenerator;
        _markdownGenerator = markdownGenerator;
    }

    public async Task<GenerationResult> GenerateAsync(
        GenerationRequest request)
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

        if (!Enum.IsDefined(request.Format))
        {
            throw new ArgumentException(
                "Неподдерживаемый формат документации.");
        }

        var outputDirectory =
            string.IsNullOrWhiteSpace(request.OutputDirectory)
                ? Path.Combine(
                    Path.GetDirectoryName(projectPath)!,
                    "docs")
                : Path.GetFullPath(request.OutputDirectory);

        var api = await _analyzer.AnalyzeAsync(projectPath);

        Directory.CreateDirectory(outputDirectory);

        string? openApiPath = null;
        string? markdownPath = null;

        if (request.Format is GenerationFormat.OpenApi
            or GenerationFormat.Both)
        {
            openApiPath = Path.Combine(
                outputDirectory,
                "openapi.json");

            var json = await _openApiGenerator.GenerateAsync(api);

            await File.WriteAllTextAsync(openApiPath, json);
        }

        if (request.Format is GenerationFormat.Markdown
            or GenerationFormat.Both)
        {
            markdownPath = Path.Combine(
                outputDirectory,
                "API.md");

            _markdownGenerator.Generate(api, markdownPath);
        }

        stopwatch.Stop();

        return new GenerationResult(
            api,
            outputDirectory,
            openApiPath,
            markdownPath,
            stopwatch.Elapsed);
    }
}

