namespace QaAgent.Core;

public sealed class RuleEngine
{
    public IReadOnlyList<RuleEvaluation> Evaluate(DocumentSnapshot snapshot, IReadOnlyList<RuleDefinition> rules)
    {
        var evaluations = new List<RuleEvaluation>();
        foreach (var rule in rules)
        {
            if (rule.Checks.Count == 0)
            {
                evaluations.Add(new RuleEvaluation(rule.RuleId, true, rule.Severity, "No executable checks found; informational only.", rule.SourceFile));
                continue;
            }

            var passed = true;
            var message = "All checks passed";
            foreach (var check in rule.Checks)
            {
                if (!EvaluateCheck(snapshot, check))
                {
                    passed = false;
                    message = check.FalseMessage ?? $"Failed check: {check.Left} {check.Op} {check.Right}";
                    break;
                }

                if (!string.IsNullOrWhiteSpace(check.TrueMessage))
                {
                    message = check.TrueMessage!;
                }
            }

            evaluations.Add(new RuleEvaluation(rule.RuleId, passed, rule.Severity, message, rule.SourceFile));
        }

        return evaluations;
    }

    private static bool EvaluateCheck(DocumentSnapshot snapshot, RuleCheck check)
    {
        var left = ResolveToken(check.Left, snapshot);
        var right = ResolveToken(check.Right, snapshot);
        var op = (check.Op ?? "=").Trim().ToLowerInvariant();

        return op switch
        {
            "=" => string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
            "!=" => !string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
            "contains" or "in" => left.Contains(right, StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
    }

    private static string ResolveToken(string token, DocumentSnapshot snapshot)
    {
        token = token.Trim();
        if (!(token.StartsWith('$') && token.EndsWith('$')))
        {
            return token;
        }

        var key = token.Trim('$').ToLowerInvariant();
        return key switch
        {
            "file_format" => snapshot.FileFormat,
            "file" => snapshot.FilePath,
            "model" => snapshot.ModelName ?? string.Empty,
            "units" => snapshot.Units ?? string.Empty,
            _ => snapshot.Attributes?.TryGetValue(key, out var value) == true ? value : string.Empty,
        };
    }
}
