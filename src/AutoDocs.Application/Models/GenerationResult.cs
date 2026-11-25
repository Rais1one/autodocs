using AutoDocs.Core.Model;

namespace AutoDocs.Application.Models;

public record GenerationResult(
    ApiDescription Api,
    string OutputDirectory,
    string? OpenApiPath,
    string? MarkdownPath,
    TimeSpan Duration
);
