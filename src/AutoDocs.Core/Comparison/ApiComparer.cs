using System.Text.RegularExpressions;
using AutoDocs.Core.Model;
using AutoDocs.Core.Normalization;

namespace AutoDocs.Core.Comparison;

// Algorithm inspired by OpenAPITools/openapi-diff.
// https://github.com/OpenAPITools/openapi-diff
public class ApiComparer
{
    public IReadOnlyList<Discrepancy> Compare(
        ApiDescription documented,
        ApiDescription current)
    {
        var differences = new List<Discrepancy>();

        var oldEndpoints = documented.Endpoints
            .ToDictionary(GetEndpointKey);

        var newEndpoints = current.Endpoints
            .ToDictionary(GetEndpointKey);

        // Удалённые эндпоинты
        foreach (var entry in oldEndpoints)
        {
            if (!newEndpoints.ContainsKey(entry.Key))
            {
                differences.Add(new Discrepancy(
                    DiscrepancyKind.EndpointRemoved,
                    DiscrepancySeverity.Error,
                    entry.Value.Key,
                    "Эндпоинт удалён из исходного кода.",
                    entry.Value.Key
                ));
            }
        }

        // Добавленные и изменённые эндпоинты
        foreach (var entry in newEndpoints)
        {
            if (!oldEndpoints.TryGetValue(
                    entry.Key, out var oldEndpoint))
            {
                differences.Add(new Discrepancy(
                    DiscrepancyKind.EndpointAdded,
                    DiscrepancySeverity.Error,
                    entry.Value.Key,
                    "Эндпоинт отсутствует в документации.",
                    null,
                    entry.Value.Key
                ));

                continue;
            }

            CompareParameters(
                oldEndpoint,
                entry.Value,
                differences);

            CompareResponses(
                oldEndpoint,
                entry.Value,
                differences);

            CompareDocumentation(
                oldEndpoint,
                entry.Value,
                differences);
        }

        return differences
            .OrderBy(d => d.EndpointKey)
            .ThenBy(d => d.Kind)
            .ToList();
    }

    private static string GetEndpointKey(Endpoint endpoint)
    {
        var route = RouteNormalizer.Normalize(endpoint.Route);

        // Имена параметров маршрута не влияют
        // на сопоставление эндпоинтов.
        route = Regex.Replace(
            route,
            @"\{([^/{}]+)\}",
            "{}");

        return $"{endpoint.HttpMethod} {route}";
    }

    private static void CompareParameters(
        Endpoint oldEndpoint,
        Endpoint newEndpoint,
        List<Discrepancy> differences)
    {
        var key = newEndpoint.Key;

        var unmatched = oldEndpoint.Parameters.ToList();

        foreach (var current in newEndpoint.Parameters)
        {
            // Имя параметра тела запроса в OpenAPI
            // не сохраняется.
            var old = unmatched.FirstOrDefault(p =>
                p.Source == current.Source &&
                (current.Source == ParameterSource.Body ||
                 p.Name == current.Name));

            // Поиск параметра с изменённым источником.
            old ??= unmatched.FirstOrDefault(p =>
                p.Name == current.Name);

            if (old is null)
            {
                differences.Add(new Discrepancy(
                    DiscrepancyKind.ParameterAdded,
                    DiscrepancySeverity.Error,
                    key,
                    $"Добавлен параметр {current.Name}.",
                    null,
                    current.Name
                ));

                continue;
            }

            unmatched.Remove(old);

            if (!TypeNormalizer.AreEquivalent(
                    old.Type, current.Type))
            {
                differences.Add(new Discrepancy(
                    DiscrepancyKind.ParameterTypeChanged,
                    DiscrepancySeverity.Error,
                    key,
                    $"Изменён тип параметра {current.Name}.",
                    old.Type,
                    current.Type
                ));
            }

            if (old.Source != current.Source)
            {
                differences.Add(new Discrepancy(
                    DiscrepancyKind.ParameterSourceChanged,
                    DiscrepancySeverity.Error,
                    key,
                    $"Изменён источник параметра {current.Name}.",
                    old.Source.ToString(),
                    current.Source.ToString()
                ));
            }

            if (old.IsRequired != current.IsRequired)
            {
                differences.Add(new Discrepancy(
                    DiscrepancyKind.ParameterRequiredChanged,
                    DiscrepancySeverity.Error,
                    key,
                    $"Изменена обязательность параметра {current.Name}.",
                    old.IsRequired.ToString(),
                    current.IsRequired.ToString()
                ));
            }

            if (!TextEquals(
                    old.Description, current.Description))
            {
                differences.Add(new Discrepancy(
                    DiscrepancyKind.ParameterDescriptionChanged,
                    DiscrepancySeverity.Warning,
                    key,
                    $"Изменено описание параметра {current.Name}.",
                    old.Description,
                    current.Description
                ));
            }
        }

        foreach (var old in unmatched)
        {
            differences.Add(new Discrepancy(
                DiscrepancyKind.ParameterRemoved,
                DiscrepancySeverity.Error,
                key,
                $"Удалён параметр {old.Name}.",
                old.Name
            ));
        }
    }

    private static void CompareResponses(
        Endpoint oldEndpoint,
        Endpoint newEndpoint,
        List<Discrepancy> differences)
    {
        var key = newEndpoint.Key;

        var oldResponses = oldEndpoint.Responses
            .ToDictionary(r => r.StatusCode);

        var newResponses = newEndpoint.Responses
            .ToDictionary(r => r.StatusCode);

        foreach (var entry in oldResponses)
        {
            if (!newResponses.ContainsKey(entry.Key))
            {
                differences.Add(new Discrepancy(
                    DiscrepancyKind.ResponseRemoved,
                    DiscrepancySeverity.Error,
                    key,
                    $"Удалён ответ HTTP {entry.Key}.",
                    entry.Key.ToString()
                ));
            }
        }

        foreach (var entry in newResponses)
        {
            if (!oldResponses.TryGetValue(
                    entry.Key, out var oldResponse))
            {
                differences.Add(new Discrepancy(
                    DiscrepancyKind.ResponseAdded,
                    DiscrepancySeverity.Error,
                    key,
                    $"Добавлен ответ HTTP {entry.Key}.",
                    null,
                    entry.Key.ToString()
                ));

                continue;
            }

            if (!TypeNormalizer.AreEquivalent(
                    oldResponse.TypeName,
                    entry.Value.TypeName))
            {
                differences.Add(new Discrepancy(
                    DiscrepancyKind.ResponseTypeChanged,
                    DiscrepancySeverity.Error,
                    key,
                    $"Изменён тип ответа HTTP {entry.Key}.",
                    oldResponse.TypeName,
                    entry.Value.TypeName
                ));
            }
        }
    }

    private static void CompareDocumentation(
        Endpoint oldEndpoint,
        Endpoint newEndpoint,
        List<Discrepancy> differences)
    {
        if (!TextEquals(
                oldEndpoint.Summary,
                newEndpoint.Summary))
        {
            differences.Add(new Discrepancy(
                DiscrepancyKind.SummaryChanged,
                DiscrepancySeverity.Warning,
                newEndpoint.Key,
                "Изменено описание эндпоинта.",
                oldEndpoint.Summary,
                newEndpoint.Summary
            ));
        }

        if (!TextEquals(
                oldEndpoint.Returns,
                newEndpoint.Returns))
        {
            differences.Add(new Discrepancy(
                DiscrepancyKind.ReturnsChanged,
                DiscrepancySeverity.Warning,
                newEndpoint.Key,
                "Изменено описание возвращаемого результата.",
                oldEndpoint.Returns,
                newEndpoint.Returns
            ));
        }
    }

    private static bool TextEquals(
        string? first,
        string? second)
    {
        return string.Equals(
            first?.Trim() ?? string.Empty,
            second?.Trim() ?? string.Empty,
            StringComparison.Ordinal);
    }
}