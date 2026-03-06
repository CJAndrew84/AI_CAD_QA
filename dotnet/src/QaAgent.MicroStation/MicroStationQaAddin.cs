using QaAgent.Core;

namespace QaAgent.MicroStation;

public interface IMicroStationHost
{
    event EventHandler<DesignFileChangedEventArgs> DesignFileChanged;
    void ShowQaDockablePane(string title, string message);
}

public sealed record DesignFileChangedEventArgs(string DgnPath, string FileFormat = "dgn8") : EventArgs;

public sealed class MicroStationQaAddin
{
    private readonly IMicroStationHost _host;
    private readonly QaCopilotOrchestrator _orchestrator;

    public MicroStationQaAddin(IMicroStationHost host, string masterXml, string rulesDirectory)
    {
        _host = host;
        _orchestrator = new QaCopilotOrchestrator(masterXml, rulesDirectory);
        _host.DesignFileChanged += OnDesignFileChanged;
    }

    private async void OnDesignFileChanged(object? sender, DesignFileChangedEventArgs e)
    {
        var snapshot = new DocumentSnapshot(
            Platform: "MicroStation",
            FilePath: e.DgnPath,
            FileFormat: e.FileFormat
        );

        var result = await _orchestrator.RunAsync(snapshot);
        var failed = result.Evaluations.Where(v => !v.Passed).ToList();

        if (failed.Count == 0)
        {
            _host.ShowQaDockablePane("CAD QA", "✅ MicroStation QA: all checks passed.");
            return;
        }

        var lines = result.Advice.Actions.Select(a => $"- {a}");
        var message = $"⚠️ MicroStation QA found {failed.Count} issue(s).\n{result.Advice.Summary}\n{string.Join("\n", lines)}";
        _host.ShowQaDockablePane("CAD QA", message);
    }
}
