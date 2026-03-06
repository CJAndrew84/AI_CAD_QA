using System.Xml.Linq;

namespace QaAgent.Core;

public sealed class XmlRuleLoader
{
    private readonly string _masterXml;
    private readonly string _rulesDirectory;

    public XmlRuleLoader(string masterXml, string rulesDirectory)
    {
        _masterXml = masterXml;
        _rulesDirectory = rulesDirectory;
    }

    public IReadOnlyList<RuleDefinition> Load()
    {
        var references = CollectReferences(_masterXml);
        var rules = new List<RuleDefinition>();

        foreach (var reference in references)
        {
            var path = ResolveReference(reference);
            if (!File.Exists(path))
            {
                continue;
            }

            rules.AddRange(ParseRuleFile(path));
        }

        return rules;
    }

    private static IReadOnlyList<string> CollectReferences(string path)
    {
        var doc = XDocument.Load(path);
        return doc.Descendants("rule")
            .Select(r => r.Element("reference")?.Value?.Trim())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Cast<string>()
            .ToList();
    }

    private string ResolveReference(string name)
    {
        var direct = Path.Combine(_rulesDirectory, name);
        if (File.Exists(direct))
        {
            return direct;
        }

        var match = Directory.EnumerateFiles(_rulesDirectory, "*.xml")
            .FirstOrDefault(file => string.Equals(Path.GetFileName(file), name, StringComparison.OrdinalIgnoreCase));

        return match ?? direct;
    }

    private static IReadOnlyList<RuleDefinition> ParseRuleFile(string path)
    {
        var doc = XDocument.Load(path);
        var root = doc.Root;
        if (root is null)
        {
            return [];
        }

        var rules = doc.Descendants("rule").ToList();
        if (root.Name.LocalName == "rule" && !rules.Contains(root))
        {
            rules.Insert(0, root);
        }

        var loaded = new List<RuleDefinition>();
        foreach (var rule in rules)
        {
            if (string.Equals((string?)rule.Attribute("id"), "_init", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var severityText = ((string?)rule.Attribute("type") ?? "advisory").ToLowerInvariant();
            var severity = severityText == "mandatory" ? RuleSeverity.Mandatory : RuleSeverity.Advisory;
            var checks = rule.Descendants("test")
                .Select(test => new RuleCheck(
                    test.Attribute("l")?.Value ?? string.Empty,
                    test.Attribute("op")?.Value ?? "=",
                    test.Attribute("r")?.Value ?? string.Empty,
                    rule.Descendants("msg").FirstOrDefault(x => (string?)x.Attribute("id") == "true")?.Value,
                    rule.Descendants("msg").FirstOrDefault(x => (string?)x.Attribute("id") == "false")?.Value
                ))
                .ToList();

            loaded.Add(new RuleDefinition(
                RuleId: (string?)rule.Attribute("id") ?? "unknown",
                Name: rule.Element("name")?.Value ?? (string?)rule.Attribute("id") ?? "unknown",
                Severity: severity,
                SourceFile: path,
                Checks: checks
            ));
        }

        return loaded;
    }
}
