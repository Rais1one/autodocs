using AutoDocs.Core.Model;
using Microsoft.OpenApi;

namespace AutoDocs.Core.Generation;

public class OpenApiGenerator
{
    public async Task<string> GenerateAsync(ApiDescription api)
    {
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = api.Title,
                Version = api.Version
            },
            Paths = new OpenApiPaths()
        };

     
        foreach (var endpoint in api.Endpoints
                     .OrderBy(e => e.Route)
                     .ThenBy(e => e.HttpMethod))
        {
            var path = endpoint.Route;

            if (!path.StartsWith('/'))
            {
                path = "/" + path;
            }

            path = path.Replace("//", "/");

            if (!document.Paths.TryGetValue(path, out var pathItem))
            {
                document.Paths[path] = pathItem =
                    new OpenApiPathItem();
            }

            var method = new System.Net.Http.HttpMethod(
                endpoint.HttpMethod.ToString().ToUpperInvariant());

            if (pathItem.Operations?.ContainsKey(method) == true)
            {
                throw new InvalidOperationException(
                    $"Duplicate operation: {method} {path}");
            }

            ((OpenApiPathItem)pathItem).AddOperation(
                method,
                CreateOperation(endpoint));
        }

        return await document.SerializeAsync(
            OpenApiSpecVersion.OpenApi3_0,
            OpenApiConstants.Json);
    }

    private static OpenApiOperation CreateOperation(
        Endpoint endpoint)
    {
        var operation = new OpenApiOperation
        {
            Summary = endpoint.Summary,
            Description = endpoint.Returns,
            Parameters = new List<IOpenApiParameter>(),
            Responses = new OpenApiResponses()
        };


        foreach (var parameter in endpoint.Parameters)
        {
            if (parameter.Source == ParameterSource.Body)
            {
                operation.RequestBody = new OpenApiRequestBody
                {
                    Description = parameter.Description,
                    Required = parameter.IsRequired,
                    Content = new Dictionary<string, IOpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = CreateSchema(parameter.Type)
                        }
                    }
                };

                continue;
            }

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = parameter.Name,
                Description = parameter.Description,

                In = parameter.Source switch
                {
                    ParameterSource.Route => ParameterLocation.Path,
                    ParameterSource.Query => ParameterLocation.Query,
                    ParameterSource.Header => ParameterLocation.Header,

                    _ => throw new NotSupportedException(
                        $"Unsupported source: {parameter.Source}")
                },

                Required = parameter.Source == ParameterSource.Route
                           || parameter.IsRequired,

                Schema = CreateSchema(parameter.Type)
            });
        }

        foreach (var response in endpoint.Responses)
        {
            var apiResponse = new OpenApiResponse
            {
                Description = $"HTTP {response.StatusCode}"
            };

            if (!string.IsNullOrWhiteSpace(response.TypeName))
            {
                apiResponse.Content =
                    new Dictionary<string, IOpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = CreateSchema(response.TypeName)
                        }
                    };
            }

            operation.Responses[response.StatusCode.ToString()] =
                apiResponse;
        }

        if (operation.Responses.Count == 0)
        {
            operation.Responses.Add("default", new OpenApiResponse
            {
                Description = "Response"
            });
        }

        return operation;
    }

    private static OpenApiSchema CreateSchema(string typeName)
    {
        typeName = typeName.Trim().TrimEnd('?');

        var (type, format) = TypeMapper.Map(typeName);

        if (type != "object")
        {
            return new OpenApiSchema
            {
                Type = type switch
                {
                    "integer" => JsonSchemaType.Integer,
                    "number" => JsonSchemaType.Number,
                    "boolean" => JsonSchemaType.Boolean,
                    "string" => JsonSchemaType.String,
                    _ => JsonSchemaType.Object
                },
                Format = format
            };
        }

        if (typeName.EndsWith("[]"))
        {
            return new OpenApiSchema
            {
                Type = JsonSchemaType.Array,
                Items = CreateSchema(typeName[..^2])
            };
        }

        string[] collectionTypes =
        {
        "IEnumerable",
        "ICollection",
        "IList",
        "List",
        "IReadOnlyList",
        "IReadOnlyCollection",
        "HashSet",
        "Collection",
        "IAsyncEnumerable"
    };

        foreach (var collection in collectionTypes)
        {
            var prefix = collection + "<";

            if (typeName.StartsWith(prefix) &&
                typeName.EndsWith(">"))
            {
                var itemType = typeName[
                    prefix.Length..^1];

                return new OpenApiSchema
                {
                    Type = JsonSchemaType.Array,
                    Items = CreateSchema(itemType)
                };
            }
        }

        return new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Title = typeName
        };
    }

}