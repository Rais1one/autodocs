using AutoDocs.Application.Models;
using AutoDocs.Core.Model;
using AutoDocs.Storage.Data;
using AutoDocs.Storage.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AutoDocs.Tests.Storage;

public class HistoryRepositoryTests : IAsyncLifetime
{
    private readonly string _directory;
    private readonly AutoDocsDbContextFactory _factory;
    private readonly HistoryRepository _repository;

    public HistoryRepositoryTests()
    {
        _directory = Path.Combine(
            Path.GetTempPath(),
            "AutoDocs.Tests",
            Guid.NewGuid().ToString());

        _factory = new AutoDocsDbContextFactory(
            Path.Combine(_directory, "history.db"));

        _repository = new HistoryRepository(_factory);
    }

    public async Task InitializeAsync()
    {
        await using var context = await _factory.CreateAsync();
    }

    public Task DisposeAsync()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }

        return Task.CompletedTask;
    }

    private CheckResult CreateResult(
        params Discrepancy[] discrepancies)
    {
        var api = new ApiDescription(
            "Test API",
            "1.0",
            Array.Empty<Endpoint>());

        return new CheckResult(
            api,
            api,
            discrepancies,
            Path.Combine(_directory, "TestApi.csproj"),
            Path.Combine(_directory, "openapi.json"),
            TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Database_IsCreated()
    {
        Assert.True(File.Exists(
            Path.Combine(_directory, "history.db")));

        await using var context = await _factory.CreateAsync();

        Assert.Empty(await context.Projects.ToListAsync());
        Assert.Empty(await context.CheckRuns.ToListAsync());
        Assert.Empty(await context.Discrepancies.ToListAsync());
        Assert.Empty(await context.ApiSnapshots.ToListAsync());
        Assert.Empty(await context.SnapshotEndpoints.ToListAsync());
    }

    [Fact]
    public async Task SaveCheckAsync_SavesCheckResult()
    {
        var result = CreateResult();

        await _repository.SaveCheckAsync(result, "CLI");

        var history = await _repository.GetHistoryAsync();

        var item = Assert.Single(history);

        Assert.Equal("TestApi", item.ProjectName);
        Assert.Equal(result.ProjectPath, item.ProjectPath);
        Assert.Equal(result.DocumentationPath, item.DocumentationPath);
        Assert.Equal("CLI", item.Source);
        Assert.Equal(0, item.ExitCode);
        Assert.Equal(2000L, item.DurationMs);
    }

    [Fact]
    public async Task SaveCheckAsync_SavesDiscrepancies()
    {
        var discrepancy = new Discrepancy(
            DiscrepancyKind.SummaryChanged,
            DiscrepancySeverity.Warning,
            "GET /api/users",
            "Изменено описание эндпоинта.",
            "Старое описание",
            "Новое описание",
            "UsersController.cs",
            18);

        var result = CreateResult(discrepancy);

        await _repository.SaveCheckAsync(result, "CLI");

        var history = await _repository.GetHistoryAsync();
        var item = Assert.Single(history);

        Assert.Equal(1, item.ExitCode);
        Assert.Equal(1, item.WarningCount);

        var differences =
            await _repository.GetDiscrepanciesAsync(item.Id);

        Assert.Equal(discrepancy, Assert.Single(differences));
    }

    [Fact]
    public async Task SaveCheckAsync_DoesNotDuplicateProject()
    {
        var result = CreateResult();

        await _repository.SaveCheckAsync(result, "CLI");
        await _repository.SaveCheckAsync(result, "WPF");

        await using var context = await _factory.CreateAsync();

        Assert.Equal(1, await context.Projects.CountAsync());
        Assert.Equal(2, await context.CheckRuns.CountAsync());
    }

    [Fact]
    public async Task SaveSnapshotAsync_SavesEndpoints()
    {
        var endpoint = new Endpoint(
            HttpVerb.Get,
            "/api/users",
            "GET /api/users",
            Array.Empty<Parameter>(),
            new[] { new ApiResponse(200, "UserDto") },
            "Получить пользователей");

        var api = new ApiDescription(
            "Test API",
            "1.0",
            new[] { endpoint });

        var result = new GenerationResult(
            api,
            _directory,
            Path.Combine(_directory, "openapi.json"),
            Path.Combine(_directory, "API.md"),
            TimeSpan.FromSeconds(1));

        await _repository.SaveSnapshotAsync(
            Path.Combine(_directory, "TestApi.csproj"),
            result);

        await using var context = await _factory.CreateAsync();

        var snapshots = await context.ApiSnapshots
            .Include(s => s.Endpoints)
            .ToListAsync();

        var snapshot = Assert.Single(snapshots);
        var savedEndpoint = Assert.Single(snapshot.Endpoints);

        Assert.Equal(api.Title, snapshot.Title);
        Assert.Equal(endpoint.Key, savedEndpoint.EndpointKey);
        Assert.Equal(endpoint.Summary, savedEndpoint.Summary);
        Assert.Contains("UserDto", savedEndpoint.ResponsesJson);
    }

    [Fact]
    public async Task SaveCheckAsync_InvalidSource_RollsBack()
    {
        var result = CreateResult();

        await Assert.ThrowsAsync<DbUpdateException>(
            () => _repository.SaveCheckAsync(result, "INVALID"));

        await using var context = await _factory.CreateAsync();

        Assert.Equal(0, await context.Projects.CountAsync());
        Assert.Equal(0, await context.CheckRuns.CountAsync());
    }
}
