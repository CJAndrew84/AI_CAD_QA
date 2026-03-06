namespace QaAgent.Core;

public interface IAiAdvisor
{
    Task<CopilotAdvice> SuggestAsync(
        DocumentSnapshot snapshot,
        IReadOnlyList<RuleEvaluation> evaluations,
        CancellationToken cancellationToken = default
    );
}
