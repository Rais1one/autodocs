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

            document.Root.Add(
                new MdHeading("Параметры", 3));

            if (endpoint.Parameters.Count == 0)
            {
                document.Root.Add(
                    new MdParagraph("Параметры отсутствуют."));
            }

            foreach (var parameter in endpoint.Parameters)
            {
                var required = parameter.IsRequired ? "Да" : "Нет";

                document.Root.Add(new MdParagraph(
                    $"{parameter.Name}: {parameter.Type}; " +
                    $"Источник: {parameter.Source}; " +
                    $"Обязательный: {required}"));
            }

            document.Root.Add(
                new MdHeading("Ответы", 3));

            foreach (var response in endpoint.Responses)
            {
                document.Root.Add(new MdParagraph(
                    $"{response.StatusCode}: " +
                    $"{response.TypeName ?? "Без тела ответа"}"));
            }
        }

        document.Save(outputPath);
    }
}