using System.ComponentModel;
using AutoDocs.Desktop.Views;
using System.Windows.Data;
using System.Diagnostics;
using AutoDocs.Application.Models;

using AutoDocs.Application.Services;

using AutoDocs.Desktop.Services;

using CommunityToolkit.Mvvm.ComponentModel;

using CommunityToolkit.Mvvm.Input;

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

    public ObservableCollection<CheckHistoryItem> HistoryItems { get; }

    = new();

    [ObservableProperty]

    [NotifyCanExecuteChangedFor(nameof(OpenHistoryResultCommand))]

    private CheckHistoryItem? _selectedHistoryItem;

    [ObservableProperty]

    private string _historyStatus = "История не загружена.";

    [ObservableProperty]

    private bool _isHistoricalResult;

    [ObservableProperty]

    private string _statusMessage = "Выберите проект для анализа.";

    [ObservableProperty]

    [NotifyCanExecuteChangedFor(nameof(SelectProjectCommand))]

    [NotifyCanExecuteChangedFor(nameof(GenerateCommand))]

    [NotifyCanExecuteChangedFor(nameof(CheckCommand))]

    [NotifyCanExecuteChangedFor(nameof(RefreshHistoryCommand))]

    [NotifyCanExecuteChangedFor(nameof(OpenHistoryResultCommand))]

    private bool _isBusy;

    [ObservableProperty]

    private int _endpointCount;

    [ObservableProperty]

    private string _outputDirectory = string.Empty;

    [ObservableProperty]

    [NotifyCanExecuteChangedFor(nameof(OpenGeneratedFileCommand))]
    private string _openApiPath = string.Empty;

    [ObservableProperty]

    [NotifyCanExecuteChangedFor(nameof(OpenGeneratedFileCommand))]
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

    [ObservableProperty]

    private string _currentPage = "Home";

    [RelayCommand]

    private async Task NavigateAsync(string page)

    {

        CurrentPage = page;

        if (page == "History" && !IsBusy)

        {

            await RefreshHistoryAsync();

        }

    }

    public ObservableCollection<Discrepancy> Discrepancies { get; }

        = new();

    public string ProjectName => HasProject

        ? Path.GetFileNameWithoutExtension(ProjectPath)

        : "Проект не выбран";

    private bool CanCheck() => HasProject && !IsBusy;

    public bool HasProject =>

        !string.IsNullOrWhiteSpace(ProjectPath);

    public ICollectionView FilteredDiscrepancies { get; }
    public string[] SeverityFilters { get; } = new[] { "Все", "Ошибки", "Предупреждения", "Информация" };

    [ObservableProperty]
    private string _selectedSeverityFilter = "Все";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenSourceFileCommand))]
    private Discrepancy? _selectedDiscrepancy;

    [ObservableProperty]
    private int _filteredCount;

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
        FilteredDiscrepancies = CollectionViewSource.GetDefaultView(Discrepancies);
        FilteredDiscrepancies.Filter = FilterDiscrepancy;
        FilteredDiscrepancies.CollectionChanged += (_, _) => UpdateFilteredCount();

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

        SelectedDiscrepancy = null;
        Discrepancies.Clear();

        ErrorCount = 0;

        WarningCount = 0;

        InfoCount = 0;

        DocumentedEndpointCount = 0;

        HasCheckResult = false;
        IsHistoricalResult = false;

        DocumentationPath = string.Empty;

        CheckStatus = "Проверка не выполнялась";

    }

    [RelayCommand(CanExecute = nameof(CanGenerate))]

    private async Task GenerateAsync()

    {

        var dialog = new GenerationDialog(OutputDirectory);
        var owner = System.Windows.Application.Current.MainWindow;
        if (owner is not null) dialog.Owner = owner;

        if (dialog.ShowDialog() != true)
        {
            StatusMessage = "Генерация отменена.";
            return;
        }

        IsBusy = true;

        StatusMessage = "Выполняется анализ проекта...";

        try

        {

            var request = new GenerationRequest(

                ProjectPath,

                dialog.OutputDirectory,

                dialog.Format);

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

        var projectDirectory = Path.GetDirectoryName(ProjectPath)!;
        var defaultPath = !string.IsNullOrWhiteSpace(DocumentationPath) && File.Exists(DocumentationPath)
            ? DocumentationPath
            : !string.IsNullOrWhiteSpace(OpenApiPath) && File.Exists(OpenApiPath)
                ? OpenApiPath
                : Path.Combine(projectDirectory, "docs", "openapi.json");
        var dialog = new CheckDialog(projectDirectory, defaultPath)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        if (dialog.ShowDialog() != true)
        {
            StatusMessage = "Проверка отменена.";
            return;
        }

        var documentationPath = dialog.DocumentationPath;
        var saveHistory = dialog.SaveHistory;
        DocumentationPath = documentationPath;
        CurrentPage = "Results";

        IsBusy = true;

        HasCheckResult = false;
        IsHistoricalResult = false;

        StatusMessage = "Выполняется проверка документации...";

        CheckStatus = "Выполняется проверка";

        SelectedDiscrepancy = null;
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

                SaveHistory: saveHistory,

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
            IsHistoricalResult = false;

            CurrentPage = "Results";

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

    private bool CanRefreshHistory() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanRefreshHistory))]

    private async Task RefreshHistoryAsync()

    {

        IsBusy = true;

        HistoryStatus = "Загрузка истории...";

        try

        {

            var items = await Task.Run(

                () => _historyService.GetHistoryAsync());

            SelectedHistoryItem = null;

            HistoryItems.Clear();

            foreach (var item in items)

            {

                HistoryItems.Add(item);

            }

            HistoryStatus = HistoryItems.Count == 0

                ? "История проверок пока пуста."

                : $"Всего проверок: {HistoryItems.Count}";

        }

        catch (Exception ex)

        {

            HistoryStatus =

                $"Не удалось загрузить историю: {ex.Message}";

        }

        finally

        {

            IsBusy = false;

        }

    }

    private bool CanOpenHistoryResult() =>

    SelectedHistoryItem is not null && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanOpenHistoryResult))]

    private async Task OpenHistoryResultAsync()

    {

        var item = SelectedHistoryItem;

        if (item is null)

        {

            return;

        }

        IsBusy = true;

        try

        {

            var discrepancies = await Task.Run(

                () => _historyService.GetDiscrepanciesAsync(item.Id));

            ProjectPath = item.ProjectPath;

            DocumentationPath = item.DocumentationPath;

            OutputDirectory = Path.Combine(

                Path.GetDirectoryName(item.ProjectPath)!,

                "docs");

            OpenApiPath = string.Empty;

            MarkdownPath = string.Empty;

            EndpointCount = item.CurrentEndpointCount;

            DocumentedEndpointCount = item.DocumentedEndpointCount;

            ErrorCount = item.ErrorCount;

            WarningCount = item.WarningCount;

            InfoCount = item.InfoCount;

            SelectedDiscrepancy = null;
            Discrepancies.Clear();

            foreach (var discrepancy in discrepancies)

            {

                Discrepancies.Add(discrepancy);

            }

            CheckStatus = item.ExitCode == 0

                ? "Документация актуальна"

                : "Обнаружены расхождения";

            HasCheckResult = true;

            IsHistoricalResult = true;

            CurrentPage = "Results";

            StatusMessage =

                $"Архивная проверка №{item.Id} от " +

                $"{item.CheckedAt.ToLocalTime():dd.MM.yyyy HH:mm}. " +

                $"Источник: {item.Source}.";

        }

        catch (Exception ex)

        {

            StatusMessage =

                $"Не удалось открыть результаты: {ex.Message}";

        }

        finally

        {

            IsBusy = false;

        }

    }

    private bool FilterDiscrepancy(object item)
    {
        if (item is not Discrepancy discrepancy) return false;
        return SelectedSeverityFilter switch
        {
            "Ошибки" => discrepancy.Severity == DiscrepancySeverity.Error,
            "Предупреждения" => discrepancy.Severity == DiscrepancySeverity.Warning,
            "Информация" => discrepancy.Severity == DiscrepancySeverity.Info,
            _ => true
        };
    }

    partial void OnSelectedSeverityFilterChanged(string value)
    {
        SelectedDiscrepancy = null;
        FilteredDiscrepancies.Refresh();
        UpdateFilteredCount();
    }

    private void UpdateFilteredCount()
    {
        int count = 0;
        foreach (var item in FilteredDiscrepancies) count++;
        FilteredCount = count;
    }

    private bool CanOpenSourceFile() => !string.IsNullOrWhiteSpace(SelectedDiscrepancy?.FilePath);

    [RelayCommand(CanExecute = nameof(CanOpenSourceFile))]
    private void OpenSourceFile()
    {
        var filePath = SelectedDiscrepancy?.FilePath;
        if (string.IsNullOrWhiteSpace(filePath)) return;
        if (!File.Exists(filePath))
        {
            StatusMessage = "Исходный файл не найден.";
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });
            StatusMessage = $"Открыт исходный файл: {filePath}. Строка: {SelectedDiscrepancy?.Line}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Не удалось открыть файл: {ex.Message}";
        }
    }
    private bool CanOpenGeneratedFile(string? path) => !string.IsNullOrWhiteSpace(path);

    [RelayCommand(CanExecute = nameof(CanOpenGeneratedFile))]
    private void OpenGeneratedFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        if (!File.Exists(path))
        {
            StatusMessage = "Файл документации не найден.";
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            StatusMessage = $"Открыт файл: {path}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Не удалось открыть файл: {ex.Message}";
        }
    }
}



