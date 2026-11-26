using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AutoDocs.Storage.Data;

public class AutoDocsDbContextFactory
{
    private readonly string _databasePath;

    public AutoDocsDbContextFactory(string? databasePath = null)
    {
        _databasePath = databasePath is null
            ? Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "AutoDocs",
                "history.db")
            : Path.GetFullPath(databasePath);
    }

    public async Task<AutoDocsDbContext> CreateAsync()
    {
        var directory = Path.GetDirectoryName(_databasePath)!;

        Directory.CreateDirectory(directory);

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            ForeignKeys = true
        }.ToString();

        var options =
            new DbContextOptionsBuilder<AutoDocsDbContext>()
                .UseSqlite(connectionString)
                .Options;

        var context = new AutoDocsDbContext(options);

        try
        {
            await context.Database.EnsureCreatedAsync();

            return context;
        }
        catch
        {
            await context.DisposeAsync();
            throw;
        }
    }
}
