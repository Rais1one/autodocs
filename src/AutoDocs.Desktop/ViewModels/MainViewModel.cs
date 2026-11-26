using AutoDocs.Application.Models;
using AutoDocs.Application.Services;
using AutoDocs.Desktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.OpenApi;
using System.IO;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using AutoDocs.Core.Model;


namespace AutoDocs.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly GenerationService _generationService;
    private readonly CheckService _checkService;
    private readonly HistoryService _historyService;
    private readonly DialogService _dialogService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProjectName))]
    [NotifyPropertyChangedFor(nameof(HasProject))]
    [NotifyCanExecuteChangedFor(nameof(GenerateCommand))]
    [NotifyCanExecuteChangedFor(nameof(CheckCommand))]
    private string _projectPath = string.Empty;


    [ObservableProperty]
    private string _statusMessage = "Выберите проект для анализа.";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SelectProjectCommand))]
    [NotifyCanExecuteChangedFor(nameof(GenerateCommand))]
    [NotifyCanExecuteChangedFor(nameof(CheckCommand))]
    private bool _isBusy;


    [ObservableProperty]
    private int _endpointCount;

    [ObservableProperty]
    private string _outputDirectory = string.Empty;

    [ObservableProperty]
    private string _openApiPath = string.Empty;

    [ObservableProperty]
    private string _markdownPath = string.Empty;

    [ObservableProperty]
    private int _errorCount;

    [ObservableProperty]
    private int _warningCount;

    [ObservableProperty]
    private int _infoCount;

    [ObservableProperty]
    private int _documentedEndpointCount;

    [ObservableProperty]
    private string _checkStatus = "Проверка не выполнялась";

    [ObservableProperty]
    private string _documentationPath = string.Empty;

    [ObservableProperty]
    private bool _hasCheckResult;

    public ObservableCollection<Discrepancy> Discrepancies { get; }
        = new();


    public string ProjectName => HasProject
        ? Path.GetFileNameWithoutExtension(ProjectPath)
        : "Проект не выбран";

    private bool CanCheck() => HasProject && !IsBusy;




    public bool HasProject =>
        !string.IsNullOrWhiteSpace(ProjectPath);

    public MainViewModel(
        GenerationService generationService,
        CheckService checkService,
        HistoryService historyService,
        DialogService dialogService)
    {
        _generationService = generationService;
        _checkService = checkService;
        _historyService = historyService;
        _dialogService = dialogService;
    }

    private bool CanSelectProject() => !IsBusy;

    private bool CanGenerate() => HasProject && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanSelectProject))]
    private void SelectProject()
    {
        var path = _dialogService.SelectProject();

        if (path is null)
        {
            return;
        }

        ProjectPath = path;
        EndpointCount = 0;
        OpenApiPath = string.Empty;
        MarkdownPath = string.Empty;

        OutputDirectory = Path.Combine(
            Path.GetDirectoryName(path)!,
            "docs");

        StatusMessage = $"Выбран проект: {ProjectName}";

        Discrepancies.Clear();

        ErrorCount = 0;
        WarningCount = 0;
        InfoCount = 0;
        DocumentedEndpointCount = 0;

        HasCheckResult = false;
        DocumentationPath = string.Empty;
        CheckStatus = "Проверка не выполнялась";

    }

    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private async Task GenerateAsync()
    {
        IsBusy = true;
        StatusMessage = "Выполняется анализ проекта...";

        try
        {
            var request = new GenerationRequest(
                ProjectPath,
                OutputDirectory,
                GenerationFormat.Both);

            var result = await Task.Run(
                () => _generationService.GenerateAsync(request));

            EndpointCount = result.Api.Endpoints.Count;
            OutputDirectory = result.OutputDirectory;
            OpenApiPath = result.OpenApiPath ?? string.Empty;
            MarkdownPath = result.MarkdownPath ?? string.Empty;

            StatusMessage = "Сохранение снимка API...";

            string? historyWarning = null;

            try
            {
                await Task.Run(() =>
                    _historyService.SaveSnapshotAsync(
                        request.ProjectPath,
                        result));
            }
            catch (Exception ex)
            {
                historyWarning =
                    $"Не удалось сохранить снимок API: {ex.Message}";
            }

            StatusMessage =
                $"Документация сформирована. " +
                $"Эндпоинтов: {EndpointCount}. " +
                $"Время: {result.Duration.TotalSeconds:F2} с.";

            if (historyWarning is not null)
            {
                StatusMessage += $"\n{historyWarning}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка генерации: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCheck))]
    private async Task CheckAsync()
    {
        var documentationPath = Path.Combine(
            Path.GetDirectoryName(ProjectPath)!,
            "docs",
            "openapi.json");

        if (!File.Exists(documentationPath))
        {
            var selectedPath =
                _dialogService.SelectDocumentation();

            if (selectedPath is null)
            {
                StatusMessage = "Проверка отменена.";
                return;
            }

            documentationPath = selectedPath;
        }

        IsBusy = true;
        HasCheckResult = false;
        StatusMessage = "Выполняется проверка документации...";
        CheckStatus = "Выполняется проверка";

        Discrepancies.Clear();

        ErrorCount = 0;
        WarningCount = 0;
        InfoCount = 0;
        DocumentedEndpointCount = 0;

        try
        {
            var request = new CheckRequest(
                ProjectPath,
                documentationPath,
                SaveHistory: true,
                Source: "WPF");

            var result = await Task.Run(
                () => _checkService.CheckAsync(request));

            DocumentationPath = result.DocumentationPath;

            EndpointCount = result.Current.Endpoints.Count;
            DocumentedEndpointCount =
                result.Documented.Endpoints.Count;

            ErrorCount = result.ErrorCount;
            WarningCount = result.WarningCount;
            InfoCount = result.InfoCount;

            foreach (var discrepancy in result.Discrepancies)
            {
                Discrepancies.Add(discrepancy);
            }

            CheckStatus = result.IsUpToDate
                ? "Документация актуальна"
                : "Обнаружены расхождения";

            HasCheckResult = true;

            StatusMessage =
                $"{CheckStatus}. " +
                $"Расхождений: {Discrepancies.Count}. " +
                $"Время: {result.Duration.TotalSeconds:F2} с.";

            if (result.HistoryWarning is not null)
            {
                StatusMessage += $"\n{result.HistoryWarning}";
            }
        }
        catch (Exception ex)
        {
            CheckStatus = "Ошибка проверки";
            StatusMessage = $"Ошибка проверки: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

}
