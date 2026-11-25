using Microsoft.Build.Locator;
using AutoDocs.Core.Analysis;
using AutoDocs.Core.Comparison;

MSBuildLocator.RegisterDefaults();

if (args.Length < 2)
{
    Console.WriteLine(
        "Использование: AutoDocs.Cli <проект.csproj> <openapi.json>");

    return 2;
}

try
{
    var projectPath = Path.GetFullPath(args[0]);
    var docsPath = Path.GetFullPath(args[1]);

    var analyzer = new ProjectAnalyzer();
    var reader = new OpenApiReader();
    var comparer = new ApiComparer();

    var current = await analyzer.AnalyzeAsync(projectPath);
    var documented = await reader.ReadAsync(docsPath);

    var differences = comparer.Compare(documented, current);

    Console.WriteLine();
    Console.WriteLine("Результаты проверки:");
    Console.WriteLine($"Эндпоинтов в коде: {current.Endpoints.Count}");
    Console.WriteLine($"Эндпоинтов в документации: {documented.Endpoints.Count}");
    Console.WriteLine($"Расхождений: {differences.Count}");

    foreach (var difference in differences)
    {
        Console.WriteLine(
            $"[{difference.Severity}] " +
            $"{difference.EndpointKey}: " +
            $"{difference.Message}");

        if (difference.FilePath is not null)
        {
            Console.WriteLine(
                $"  Файл: {difference.FilePath}:{difference.Line}");
        }

        if (difference.OldValue is not null)
            Console.WriteLine($"  Было: {difference.OldValue}");

        if (difference.NewValue is not null)
            Console.WriteLine($"  Стало: {difference.NewValue}");
    }

    if (differences.Count == 0)
    {
        Console.WriteLine("Документация актуальна.");
        return 0;
    }

    Console.WriteLine("Документация неактуальна.");
    return 1;
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка: {ex.Message}");
    return 2;
}


