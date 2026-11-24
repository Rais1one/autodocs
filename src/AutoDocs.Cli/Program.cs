using Microsoft.Build.Locator;
using AutoDocs.Core.Analysis;
using AutoDocs.Core.Generation;

var instance = MSBuildLocator.RegisterDefaults();
Console.WriteLine($"MSBuild: {instance.Name} {instance.Version}");

if (args.Length == 0)
{
    Console.WriteLine("Использование: AutoDocs.Cli <путь к файлу .csproj>");
    return 2;
}

var projectPath = Path.GetFullPath(args[0]);

if (!File.Exists(projectPath))
{
    Console.WriteLine($"Файл не найден: {projectPath}");
    return 2;
}

// Анализ проекта
var analyzer = new ProjectAnalyzer();
var apiDescription = await analyzer.AnalyzeAsync(projectPath);

// Создание папки для документации
var outputDirectory = Path.Combine(
    Path.GetDirectoryName(projectPath)!, "docs");

Directory.CreateDirectory(outputDirectory);

// Генерация OpenAPI
var openApiGenerator = new OpenApiGenerator();
var json = await openApiGenerator.GenerateAsync(apiDescription);

var openApiPath = Path.Combine(outputDirectory, "openapi.json");
await File.WriteAllTextAsync(openApiPath, json);

// Генерация Markdown
var markdownGenerator = new MarkdownGenerator();

var markdownPath = Path.Combine(outputDirectory, "API.md");
markdownGenerator.Generate(apiDescription, markdownPath);

Console.WriteLine($"OpenAPI: {openApiPath}");
Console.WriteLine($"Markdown: {markdownPath}");

return 0;