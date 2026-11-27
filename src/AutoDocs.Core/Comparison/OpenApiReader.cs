using AutoDocs.Core.Model;
using AutoDocs.Core.Normalization;
using Microsoft.OpenApi;

namespace AutoDocs.Core.Comparison;

public class OpenApiReader
{
    public async Task<ApiDescription> ReadAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "Файл документации не найден.",
                filePath);
        }

        await using var stream = File.OpenRead(filePath);

        var (document, diagnostic) =
            await OpenApiDocument.LoadAsync(stream, "json");

        if (document is null)
        {
            throw new InvalidDataException(
                "Не удалось прочитать OpenAPI-документ.");
        }

        if (diagnostic?.Errors.Count > 0)
        {
            throw new InvalidDataException(
                "OpenAPI-документ содержит ошибки: " +
                string.Join("; ", diagnostic.Errors));
        }

        var endpoints = new List<Endpoint>();

        foreach (var path in document.Paths)
        {
            if (path.Value.Operations is null)
                continue;

            foreach (var operationEntry in path.Value.Operations)
            {
                if (!Enum.TryParse<HttpVerb>(
                    operationEntry.Key.Method,
                    true,
                    out var verb))
                {
                    continue;
                }

                var operation = operationEntry.Value;
                var route = RouteNormalizer.Normalize(path.Key);

                var key =
                    $"{verb.ToString().ToUpperInvariant()} {route}";

                var parameters = new List<Parameter>();
                var responses = new List<ApiResponse>();

                var effective = (operation.Parameters ?? []).Concat(path.Value.Parameters ?? [])
                    .DistinctBy(p => (p.In, p.Name));
                foreach (var parameter in effective)
                {
                    ParameterSource? source = parameter.In switch
                    {
                        ParameterLocation.Path =>
                            ParameterSource.Route,

                        ParameterLocation.Query =>
                            ParameterSource.Query,

                        ParameterLocation.Header =>
                            ParameterSource.Header,

                        _ => null
                    };

                    if (source is null)
                        continue;

                    parameters.Add(new Parameter(
                        parameter.Name ?? "",
                        ReadSchema(parameter.Schema),
                        source.Value,
                        parameter.Required,
                        parameter.Description
                    )
                    { Schema = parameter.Schema });
                }

                if (operation.RequestBody is not null &&
                    operation.RequestBody.Content is not null &&
                    operation.RequestBody.Content.TryGetValue(
                        "application/json", out var body))
                {
                    parameters.Add(new Parameter(
                        "body",
                        ReadSchema(body.Schema),
                        ParameterSource.Body,
                        operation.RequestBody.Required,
                        operation.RequestBody.Description
                    )
                    { Schema = body.Schema });
                }

                if (operation.Responses is not null)
                {
                    foreach (var response in operation.Responses)
                    {
                        if (!int.TryParse(
                            response.Key, out var statusCode))
                        {
                            continue;
                        }

                        IOpenApiSchema? responseSchema = null;

                        if (response.Value.Content is not null &&
                            response.Value.Content.TryGetValue(
                                "application/json", out var content))
                        {
                            responseSchema = content.Schema;
                        }

                        responses.Add(new ApiResponse(
                            statusCode,
                            responseSchema is null ? null : ReadSchema(responseSchema)
                        )
                        { Schema = responseSchema });
                    }
                }

                endpoints.Add(new Endpoint(
                    verb,
                    route,
                    key,
                    parameters,
                    responses,
                    operation.Summary,
                    operation.Description
                ));
            }
        }

        return new ApiDescription(
            document.Info?.Title ?? "Unknown API",
            document.Info?.Version ?? "1.0.0",
            endpoints
            )
        { Components = document.Components };

    }

    private static string ReadSchema(IOpenApiSchema? schema)
    {
        if (schema is null)
            return "object";

        return schema.Type switch
        {
            JsonSchemaType.Integer =>
                schema.Format == "int64" ? "long" : "int",

            JsonSchemaType.Number =>
                schema.Format == "float" ? "float" : "double",

            JsonSchemaType.Boolean => "bool",

            JsonSchemaType.String => schema.Format switch
            {
                "uuid" => "Guid",
                "date-time" => "DateTime",
                "date" => "DateOnly",
                "time" => "TimeOnly",
                "byte" => "byte[]",
                _ => "string"
            },

            JsonSchemaType.Array =>
                $"IEnumerable<{ReadSchema(schema.Items)}>",

            JsonSchemaType.Object =>
                schema.Title ?? "object",

            _ => schema.Title ?? "object"
        };
    }
}