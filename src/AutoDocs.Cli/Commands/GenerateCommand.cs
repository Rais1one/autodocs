using AutoDocs.Application.Models;
using AutoDocs.Application.Services;
using AutoDocs.Cli.Settings;
using AutoDocs.Core.Analysis;
using AutoDocs.Core.Generation;
using Spectre.Console.Cli;

namespace AutoDocs.Cli.Commands;

public class GenerateCommand : AsyncCommand<GenerateSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        GenerateSettings settings,
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

            var service = new GenerationService(
                new ProjectAnalyzer(),
                new OpenApiGenerator(),
                new MarkdownGenerator());

            var request = new GenerationRequest(
                settings.ProjectPath,
                settings.OutputDirectory);

            var result = await service.GenerateAsync(request);

            Console.WriteLine();
            Console.WriteLine("Документация успешно сформирована.");
            Console.WriteLine($"Эндпоинтов: {result.Api.Endpoints.Count}");
            Console.WriteLine($"OpenAPI: {result.OpenApiPath}");
            Console.WriteLine($"Markdown: {result.MarkdownPath}");
            Console.WriteLine($"Время: {result.Duration.TotalSeconds:F2} с.");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Ошибка: {ex.Message}");
            return 2;
        }
    }

    

}
