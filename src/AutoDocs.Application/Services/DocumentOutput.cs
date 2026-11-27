namespace AutoDocs.Application.Services;


public sealed class DocumentOutput : IDisposable
{
    private readonly string directory;
    private readonly string staging;
    private readonly List<string> files = [];
    private bool keepBackup;

    public DocumentOutput(string directory)
    {
        this.directory = directory;
        Directory.CreateDirectory(directory);
        staging = Path.Combine(directory, $".autodocs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(staging);
    }

    public string Stage(string name)
    {
        files.Add(name);
        return Path.Combine(staging, name);
    }

    public void Commit()
    {
        using var writeLock = new FileStream(Path.Combine(directory, ".autodocs.lock"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
        foreach (var name in files)
            if (Directory.Exists(Path.Combine(directory, name)))
                throw new IOException($"Путь выходного файла занят каталогом: {name}");

        var published = new Stack<(string Target, string? Backup)>();
        try
        {
            foreach (var name in files)
            {
                var target = Path.Combine(directory, name);
                var source = Path.Combine(staging, name);
                string? backup = null;
                if (File.Exists(target))
                {
                    backup = source + ".bak";
                    File.Replace(source, target, backup);
                }
                else File.Move(source, target);
                published.Push((target, backup));
            }
        }
        catch (Exception failure)
        {
            try
            {
                foreach (var (target, backup) in published)
                {
                    if (backup is null) File.Delete(target);
                    else File.Move(backup, target, overwrite: true);
                }
            }
            catch (Exception recovery)
            {
                keepBackup = true;
                throw new IOException($"Не удалось восстановить документацию. Резервные файлы: {staging}",
                    new AggregateException(failure, recovery));
            }
            throw;
        }
    }

    public void Dispose()
    {
        if (!keepBackup) Directory.Delete(staging, recursive: true);
    }
}
