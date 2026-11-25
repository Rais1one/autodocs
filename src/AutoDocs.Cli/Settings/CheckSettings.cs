using System.ComponentModel;
using Spectre.Console.Cli;

namespace AutoDocs.Cli.Settings;

public class CheckSettings : CommandSettings
{
    [CommandOption("-p|--project <PATH>")]
    [Description("Путь к файлу проекта .csproj")]
    public string ProjectPath { get; init; } = string.Empty;

    [CommandOption("-d|--docs <PATH>")]
    [Description("Путь к существующему openapi.json")]
    public string? DocumentationPath { get; init; }

    [CommandOption("--no-history")]
    [Description("Не сохранять результат в историю")]
    public bool NoHistory { get; init; }
}
