using System.Windows;
using AutoDocs.Application.Services;
using AutoDocs.Core.Analysis;
using AutoDocs.Core.Comparison;
using AutoDocs.Core.Generation;
using AutoDocs.Desktop.ViewModels;
using AutoDocs.Storage.Data;
using AutoDocs.Storage.Repositories;
using AutoDocs.Desktop.Services;
using Microsoft.Build.Locator;

namespace AutoDocs.Desktop;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }

            var historyRepository = new HistoryRepository(
                new AutoDocsDbContextFactory());

            var historyService = new HistoryService(
                historyRepository);

            var generationService = new GenerationService(
                new ProjectAnalyzer(),
                new OpenApiGenerator(),
                new MarkdownGenerator());

            var checkService = new CheckService(
                new ProjectAnalyzer(),
                new OpenApiReader(),
                new ApiComparer(),
                historyService);

            var dialogService = new DialogService();

            var viewModel = new MainViewModel(
                generationService,
                checkService,
                historyService,
                dialogService);


            MainWindow = new MainWindow
            {
                DataContext = viewModel
            };

            MainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Ошибка запуска AutoDocs:\n{ex.Message}",
                "AutoDocs",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(2);
        }
    }
}
