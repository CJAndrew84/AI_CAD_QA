namespace QaAgent.Core;

public enum RuleSeverity
{
    Mandatory,
    Advisory
}

public sealed record RuleCheck(string Left, string Op, string Right, string? TrueMessage, string? FalseMessage);

public sealed record RuleDefinition(
    string RuleId,
    string Name,
    RuleSeverity Severity,
    string SourceFile,
    IReadOnlyList<RuleCheck> Checks
);

public sealed record RuleEvaluation(
    string RuleId,
    bool Passed,
    RuleSeverity Severity,
    string Message,
    string SourceFile
);

public sealed record DocumentSnapshot(
    string Platform,
    string FilePath,
    string FileFormat,
    string? Units = null,
    string? ModelName = null,
    IReadOnlyDictionary<string, string>? Attributes = null
);

public sealed record CopilotAdvice(string Summary, IReadOnlyList<string> Actions);

public sealed record QaResult(IReadOnlyList<RuleEvaluation> Evaluations, CopilotAdvice Advice);
