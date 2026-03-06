namespace QaAgent.Core;

public sealed class QaCopilotOrchestrator
{
    private readonly XmlRuleLoader _loader;
    private readonly RuleEngine _engine;
    private readonly MsCopilotClient _copilot;

    public QaCopilotOrchestrator(string masterXml, string rulesDirectory, MsCopilotClient? copilot = null)
    {
        _loader = new XmlRuleLoader(masterXml, rulesDirectory);
        _engine = new RuleEngine();
        _copilot = copilot ?? new MsCopilotClient();
    }

    public async Task<QaResult> RunAsync(DocumentSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var rules = _loader.Load();
        var evaluations = _engine.Evaluate(snapshot, rules);
        var advice = await _copilot.SuggestAsync(snapshot, evaluations, cancellationToken);
        return new QaResult(evaluations, advice);
    }
}
