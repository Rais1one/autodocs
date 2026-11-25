namespace AutoDocs.Application.Models;

public record CheckRequest(
    string ProjectPath,
    string? DocumentationPath = null,
    bool SaveHistory = true
);
