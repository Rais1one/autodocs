namespace AutoDocs.Application.Models;

public record GenerationRequest(
    string ProjectPath,
    string? OutputDirectory = null,
    GenerationFormat Format = GenerationFormat.Both
);
