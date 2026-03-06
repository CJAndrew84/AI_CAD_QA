using QaAgent.Core;

namespace QaAgent.AutoCAD;

public interface IAutoCadHost
{
    event EventHandler<DocumentChangedEventArgs> DocumentChanged;
    void ShowQaPanel(string title, string message);
}

public sealed record DocumentChangedEventArgs(string DrawingPath, string FileFormat = "dwg") : EventArgs;

public sealed class AutoCadQaAddin
{
    private readonly IAutoCadHost _host;
    private readonly QaCopilotOrchestrator _orchestrator;

    public AutoCadQaAddin(IAutoCadHost host, string masterXml, string rulesDirectory)
    {
        _host = host;
        _orchestrator = new QaCopilotOrchestrator(masterXml, rulesDirectory);
        _host.DocumentChanged += OnDocumentChanged;
    }

    private async void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
    {
        var snapshot = new DocumentSnapshot(
            Platform: "AutoCAD",
            FilePath: e.DrawingPath,
            FileFormat: e.FileFormat
        );

        var result = await _orchestrator.RunAsync(snapshot);
        var failed = result.Evaluations.Where(v => !v.Passed).ToList();

        if (failed.Count == 0)
        {
            _host.ShowQaPanel("CAD QA", "✅ AutoCAD QA: all checks passed.");
            return;
        }

        var lines = result.Advice.Actions.Select(a => $"- {a}");
        var message = $"⚠️ AutoCAD QA found {failed.Count} issue(s).\n{result.Advice.Summary}\n{string.Join("\n", lines)}";
        _host.ShowQaPanel("CAD QA", message);
    }
}
