using System;
using System.IO;
using System.Windows;
using AutoDocs.Application.Models;
using Microsoft.Win32;

namespace AutoDocs.Desktop.Views;

public partial class GenerationDialog : Window
{
    private readonly string _defaultDirectory;
    public string OutputDirectory { get; private set; }
    public GenerationFormat Format { get; private set; } = GenerationFormat.Both;

    public GenerationDialog(string outputDirectory)
    {
        InitializeComponent();
        _defaultDirectory = outputDirectory;
        OutputDirectory = outputDirectory;
        OutputBox.Text = outputDirectory;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Выберите папку сохранения" };
        if (Directory.Exists(OutputBox.Text)) dialog.InitialDirectory = OutputBox.Text;
        if (dialog.ShowDialog(this) == true) OutputBox.Text = dialog.FolderName;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        bool openApi = OpenApiBox.IsChecked == true;
        bool markdown = MarkdownBox.IsChecked == true;
        if (!openApi && !markdown)
        {
            ErrorText.Text = "Выберите хотя бы один формат документации.";
            return;
        }
        try
        {
            var path = OutputBox.Text.Trim();
            OutputDirectory = Path.GetFullPath(string.IsNullOrWhiteSpace(path) ? _defaultDirectory : path, Path.GetDirectoryName(_defaultDirectory)!);
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Некорректный путь: {ex.Message}";
            return;
        }
        Format = openApi && markdown ? GenerationFormat.Both : openApi ? GenerationFormat.OpenApi : GenerationFormat.Markdown;
        DialogResult = true;
    }
}



