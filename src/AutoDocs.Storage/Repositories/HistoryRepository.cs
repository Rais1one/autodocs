using System.Text.Json;
using AutoDocs.Application.Interfaces;
using AutoDocs.Application.Models;
using AutoDocs.Core.Model;
using AutoDocs.Storage.Data;
using AutoDocs.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoDocs.Storage.Repositories;

public class HistoryRepository : IHistoryRepository
{
    private readonly AutoDocsDbContextFactory _factory;

    public HistoryRepository(AutoDocsDbContextFactory factory)
    {
        _factory = factory;
    }

    public async Task SaveCheckAsync(
        CheckResult result,
        string source)
    {
        await using var context = await _factory.CreateAsync();

        var project = await GetOrCreateProjectAsync(
            context,
            result.ProjectPath);

        var checkRun = new CheckRunEntity
        {
            Project = project,
            CheckedAt = DateTime.UtcNow,
            DocumentationPath = result.DocumentationPath,
            Source = source,
            ExitCode = result.IsUpToDate ? 0 : 1,
            ErrorCount = result.ErrorCount,
            WarningCount = result.WarningCount,
            InfoCount = result.InfoCount,
            CurrentEndpointCount = result.Current.Endpoints.Count,
            DocumentedEndpointCount = result.Documented.Endpoints.Count,
            DurationMs = (long)result.Duration.TotalMilliseconds
        };

        foreach (var difference in result.Discrepancies)
        {
            checkRun.Discrepancies.Add(new DiscrepancyEntity
            {
                Kind = difference.Kind,
                Severity = difference.Severity,
                EndpointKey = difference.EndpointKey,
                Message = difference.Message,
                OldValue = difference.OldValue,
                NewValue = difference.NewValue,
                FilePath = difference.FilePath,
                Line = difference.Line
            });
        }

        context.CheckRuns.Add(checkRun);

        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<CheckHistoryItem>> GetHistoryAsync()
    {
        await using var context = await _factory.CreateAsync();

        return await context.CheckRuns
            .AsNoTracking()
            .OrderByDescending(run => run.CheckedAt)
            .ThenByDescending(run => run.Id)
            .Select(run => new CheckHistoryItem(
                run.Id,
                run.Project.Name,
                run.Project.ProjectPath,
                run.DocumentationPath,
                run.CheckedAt,
                run.Source,
                run.ExitCode,
                run.ErrorCount,
                run.WarningCount,
                run.InfoCount,
                run.CurrentEndpointCount,
                run.DocumentedEndpointCount,
                run.DurationMs))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Discrepancy>> GetDiscrepanciesAsync(
        int checkRunId)
    {
        await using var context = await _factory.CreateAsync();

        return await context.Discrepancies
            .AsNoTracking()
            .Where(d => d.CheckRunId == checkRunId)
            .OrderBy(d => d.Id)
            .Select(d => new Discrepancy(
                d.Kind,
                d.Severity,
                d.EndpointKey,
                d.Message,
                d.OldValue,
                d.NewValue,
                d.FilePath,
                d.Line))
            .ToListAsync();
    }

    public async Task SaveSnapshotAsync(
        string projectPath,
        GenerationResult result)
    {
        await using var context = await _factory.CreateAsync();

        var project = await GetOrCreateProjectAsync(
            context,
            projectPath);

        var snapshot = new ApiSnapshotEntity
        {
            Project = project,
            CreatedAt = DateTime.UtcNow,
            Title = result.Api.Title,
            Version = result.Api.Version,
            OpenApiPath = result.OpenApiPath,
            MarkdownPath = result.MarkdownPath
        };

        foreach (var endpoint in result.Api.Endpoints)
        {
            snapshot.Endpoints.Add(new SnapshotEndpointEntity
            {
                HttpMethod = endpoint.HttpMethod,
                Route = endpoint.Route,
                EndpointKey = endpoint.Key,
                Summary = endpoint.Summary,
                Returns = endpoint.Returns,
                ParametersJson = JsonSerializer.Serialize(
                    endpoint.Parameters),
                ResponsesJson = JsonSerializer.Serialize(
                    endpoint.Responses),
                FilePath = endpoint.FilePath,
                Line = endpoint.Line
            });
        }

        context.ApiSnapshots.Add(snapshot);

        await context.SaveChangesAsync();
    }

    private static async Task<ProjectEntity> GetOrCreateProjectAsync(
        AutoDocsDbContext context,
        string projectPath)
    {
        projectPath = Path.GetFullPath(projectPath);

        var project = await context.Projects
            .FirstOrDefaultAsync(p => p.ProjectPath == projectPath);

        if (project is not null)
        {
            return project;
        }

        project = new ProjectEntity
        {
            Name = Path.GetFileNameWithoutExtension(projectPath),
            ProjectPath = projectPath
        };

        context.Projects.Add(project);

        return project;
    }
}
