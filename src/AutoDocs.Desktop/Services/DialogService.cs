using Microsoft.Win32;

namespace AutoDocs.Desktop.Services;

public class DialogService
{
    public string? SelectProject()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Выберите проект ASP.NET Core",
            Filter = "Проекты C# (*.csproj)|*.csproj",
            DefaultExt = ".csproj",
            CheckFileExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog() == true
            ? dialog.FileName
            : null;
    }

    public string? SelectDocumentation()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Выберите OpenAPI-документацию",
            Filter = "JSON-файлы (*.json)|*.json",
            DefaultExt = ".json",
            CheckFileExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog() == true
            ? dialog.FileName
            : null;
    }

    public string? SelectDirectory()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Выберите папку сохранения",
            Multiselect = false
        };

        return dialog.ShowDialog() == true
            ? dialog.FolderName
            : null;
    }
}
