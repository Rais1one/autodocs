using AutoDocs.Core.Model;
using Grynwald.MarkdownGenerator;

namespace AutoDocs.Core.Generation;

public class MarkdownGenerator
{
    public void Generate(ApiDescription api, string outputPath)
    {
        var document = new MdDocument();

        document.Root.Add(new MdHeading(api.Title, 1));

        document.Root.Add(
            new MdParagraph($"Версия API: {api.Version}"));

        foreach (var endpoint in api.Endpoints
                     .OrderBy(e => e.Route)
                     .ThenBy(e => e.HttpMethod))
        {
            var method = endpoint.HttpMethod
                .ToString().ToUpperInvariant();

            document.Root.Add(
                new MdHeading($"{method} {endpoint.Route}", 2));

           
            if (!string.IsNullOrWhiteSpace(endpoint.Summary))
            {
                document.Root.Add(
                    new MdParagraph(endpoint.Summary));
            }

            document.Root.Add(new MdHeading("Параметры", 3));

            if (endpoint.Parameters.Count == 0)
            {
                document.Root.Add(
                    new MdParagraph("Параметры отсутствуют."));
            }

            foreach (var parameter in endpoint.Parameters)
            {
                var required = parameter.IsRequired ? "Да" : "Нет";

                var text =
                    $"{parameter.Name}: {parameter.Type}; " +
                    $"Источник: {parameter.Source}; " +
                    $"Обязательный: {required}";

                if (!string.IsNullOrWhiteSpace(parameter.Description))
                {
                    text += $"; Описание: {parameter.Description}";
                }

                document.Root.Add(new MdParagraph(text));
            }

            document.Root.Add(new MdHeading("Ответы", 3));

            if (!string.IsNullOrWhiteSpace(endpoint.Returns))
            {
                document.Root.Add(
                    new MdParagraph(endpoint.Returns));
            }

            foreach (var response in endpoint.Responses)
            {
                document.Root.Add(new MdParagraph(
                    $"{response.StatusCode}: " +
                    $"{response.TypeName ?? "Без тела ответа"}"));
            }
        }

        if (api.Components?.Schemas is { Count: > 0 } schemas)
        {
            document.Root.Add(new MdHeading("Модели данных", 2));
            foreach (var (name, schema) in schemas.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                document.Root.Add(new MdHeading(schema.Title ?? name, 3));
                if (schema.Properties is null) continue;
                foreach (var (propertyName, property) in schema.Properties)
                {
                    var required = schema.Required?.Contains(propertyName) == true ? "да" : "нет";
                    document.Root.Add(new MdParagraph(
                        $"{propertyName}: {SchemaType(property)}. Обязательное поле: {required}."));
                }
            }
        }

        document.Save(outputPath);
    }

    private static string SchemaType(Microsoft.OpenApi.IOpenApiSchema schema)
    {
        if (schema.Title is not null) return schema.Title;
        if (schema.Items is not null) return $"массив ({schema.Items.Title ?? schema.Items.Type?.ToString()})";
        return schema.Format ?? schema.Type?.ToString()?.ToLowerInvariant() ?? "object";
    }
}
