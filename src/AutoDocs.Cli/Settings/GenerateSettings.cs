using System.ComponentModel;
using Spectre.Console.Cli;

namespace AutoDocs.Cli.Settings;

public class GenerateSettings : CommandSettings
{
    [CommandOption("-p|--project <PATH>")]
    [Description("Путь к файлу проекта .csproj")]
    public string ProjectPath { get; init; } = string.Empty;

    [CommandOption("-o|--output <PATH>")]
    [Description("Папка для сохранения документации")]
    public string? OutputDirectory { get; init; }
}
