namespace QaAgent.Core;

public sealed class QaCopilotOrchestrator
{
    private readonly XmlRuleLoader _loader;
    private readonly RuleEngine _engine;
    private readonly IAiAdvisor _advisor;

    public QaCopilotOrchestrator(string masterXml, string rulesDirectory, IAiAdvisor? advisor = null)
    {
        _loader = new XmlRuleLoader(masterXml, rulesDirectory);
        _engine = new RuleEngine();
        _advisor = advisor ?? AiAdvisorFactory.CreateDefault();
    }

    public async Task<QaResult> RunAsync(DocumentSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var rules = _loader.Load();
        var evaluations = _engine.Evaluate(snapshot, rules);
        var advice = await _advisor.SuggestAsync(snapshot, evaluations, cancellationToken);
        return new QaResult(evaluations, advice);
    }
}
