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
                .Select(test =>
                {
                    // Prefer attribute-based operands; fall back to nested elements if needed.
                    var left = test.Attribute("l")?.Value;
                    if (string.IsNullOrWhiteSpace(left))
                    {
                        left = test.Element("l")?.Value;
                    }
                    left ??= string.Empty;

                    var right = test.Attribute("r")?.Value;
                    if (string.IsNullOrWhiteSpace(right))
                    {
                        right = test.Element("r")?.Value;
                    }
                    right ??= string.Empty;

                    // If both operands are empty, this test likely uses an unsupported encoding; skip it.
                    if (string.IsNullOrWhiteSpace(left) && string.IsNullOrWhiteSpace(right))
                    {
                        return null;
                    }

                    var op = test.Attribute("op")?.Value ?? "=";
                    var trueMessage = rule.Descendants("msg").FirstOrDefault(x => (string?)x.Attribute("id") == "true")?.Value;
                    var falseMessage = rule.Descendants("msg").FirstOrDefault(x => (string?)x.Attribute("id") == "false")?.Value;

                    return new RuleCheck(
                        left,
                        op,
                        right,
                        trueMessage,
                        falseMessage
                    );
                })
                .Where(check => check is not null)
                .Cast<RuleCheck>()
                .ToList();

            loaded.Add(new RuleDefinition(
                RuleId: (string?)rule.Attribute("id") ?? Path.GetFileNameWithoutExtension(path),
                Name: rule.Element("name")?.Value ?? (string?)rule.Attribute("id") ?? Path.GetFileNameWithoutExtension(path),
                Severity: severity,
                SourceFile: path,
                Checks: checks
            ));
        }

        return loaded;
    }
}
