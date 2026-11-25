using AutoDocs.Application.Models;
using AutoDocs.Application.Services;
using AutoDocs.Cli.Settings;
using AutoDocs.Core.Analysis;
using AutoDocs.Core.Comparison;
using Spectre.Console.Cli;

namespace AutoDocs.Cli.Commands;

public class CheckCommand : AsyncCommand<CheckSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CheckSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(settings.ProjectPath))
            {
                Console.Error.WriteLine(
                    "Укажите путь к проекту через --project.");

                return 2;
            }

            var service = new CheckService(
                new ProjectAnalyzer(),
                new OpenApiReader(),
                new ApiComparer());

            var request = new CheckRequest(
                settings.ProjectPath,
                settings.DocumentationPath,
                !settings.NoHistory);

            var result = await service.CheckAsync(request);

            Console.WriteLine();
            Console.WriteLine("Результаты проверки:");
            Console.WriteLine($"Эндпоинтов в коде: {result.Current.Endpoints.Count}");
            Console.WriteLine($"Эндпоинтов в документации: {result.Documented.Endpoints.Count}");
            Console.WriteLine($"Расхождений: {result.Discrepancies.Count}");
            Console.WriteLine($"Ошибок: {result.ErrorCount}");
            Console.WriteLine($"Предупреждений: {result.WarningCount}");
            Console.WriteLine($"Время: {result.Duration.TotalSeconds:F2} с.");

            foreach (var difference in result.Discrepancies)
            {
                Console.WriteLine();

                Console.WriteLine(
                    $"[{difference.Severity}] " +
                    $"{difference.EndpointKey}: {difference.Message}");

                if (difference.FilePath is not null)
                    Console.WriteLine(
                        $"  Файл: {difference.FilePath}:{difference.Line}");

                if (difference.OldValue is not null)
                    Console.WriteLine($"  Было: {difference.OldValue}");

                if (difference.NewValue is not null)
                    Console.WriteLine($"  Стало: {difference.NewValue}");
            }

            Console.WriteLine();

            Console.WriteLine(result.IsUpToDate
                ? "Документация актуальна."
                : "Документация неактуальна.");

            return result.IsUpToDate ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Ошибка: {ex.Message}");
            return 2;
        }
    }
}
