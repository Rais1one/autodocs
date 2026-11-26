using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace AutoDocs.Desktop.Views;

public partial class CheckDialog : Window
{
    private readonly string _projectDirectory;
    public string DocumentationPath { get; private set; }
    public bool SaveHistory { get; private set; } = true;

    public CheckDialog(string projectDirectory, string defaultPath)
    {
        InitializeComponent();
        _projectDirectory = projectDirectory;
        DocumentationPath = defaultPath;
        DocumentationBox.Text = defaultPath;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Выберите OpenAPI-документацию",
            Filter = "JSON-файлы (*.json)|*.json",
            DefaultExt = ".json",
            CheckFileExists = true,
            Multiselect = false,
            InitialDirectory = _projectDirectory
        };
        if (dialog.ShowDialog(this) == true)
        {
            DocumentationBox.Text = dialog.FileName;
            ErrorText.Text = string.Empty;
        }
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var input = DocumentationBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(input))
            {
                ErrorText.Text = "Укажите файл документации.";
                return;
            }
            var path = Path.GetFullPath(input, _projectDirectory);
            if (!string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
            {
                ErrorText.Text = "Укажите существующий JSON-файл.";
                return;
            }
            DocumentationPath = path;
            SaveHistory = SaveHistoryBox.IsChecked == true;
            DialogResult = true;
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Некорректный путь: {ex.Message}";
        }
    }
}



