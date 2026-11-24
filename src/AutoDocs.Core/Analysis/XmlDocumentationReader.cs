using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace AutoDocs.Core.Analysis;

public static class XmlDocumentationReader
{
    public static XElement? Read(ISymbol? symbol)
    {
        var xml = symbol?.GetDocumentationCommentXml();

        if (string.IsNullOrWhiteSpace(xml))
            return null;

        try
        {
            return XElement.Parse(xml);
        }
        catch (XmlException)
        {
            return null;
        }
    }

    public static string? GetSummary(XElement? documentation)
    {
        return documentation?
            .Element("summary")?
            .Value.Trim();
    }

    public static string? GetReturns(XElement? documentation)
    {
        return documentation?
            .Element("returns")?
            .Value.Trim();
    }

    public static string? GetParameter(
        XElement? documentation,
        string parameterName)
    {
        return documentation?
            .Elements("param")
            .FirstOrDefault(p =>
                (string?)p.Attribute("name") == parameterName)?
            .Value.Trim();
    }
}
