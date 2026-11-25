using AutoDocs.Cli.Commands;
using Microsoft.Build.Locator;
using Spectre.Console.Cli;

try
{
    MSBuildLocator.RegisterDefaults();

    var app = new CommandApp();

    app.Configure(config =>
    {
        config.SetApplicationName("autodocs");

        config.AddCommand<GenerateCommand>("generate")
            .WithDescription("Сформировать документацию REST API");

        config.AddCommand<CheckCommand>("check")
            .WithDescription("Проверить актуальность документации");
    });

    var exitCode = await app.RunAsync(args);

    return exitCode < 0 ? 2 : exitCode;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Ошибка: {ex.Message}");
    return 2;
}
