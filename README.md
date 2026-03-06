# PW_CAD_QA – AI CAD QA Agent

This repo now includes both:

- a **Python prototype** for XML QA evaluation + Copilot guidance, and
- **.NET add-on scaffolding** for AutoCAD and MicroStation hosts.

The goal is an embedded AI CAD QA agent that watches design activity and provides rule-compliance guidance from your XML QA rules.

## Existing QA rule sources

- Master: `Rule XML Masters/Master.xml`
- Referenced rule packs: `Rule XML Files/*.xml`

## .NET add-ons (new)

### Projects

- `dotnet/src/QaAgent.Core`
  - XML loader (`XmlRuleLoader`) for `Master.xml` and referenced XML files.
  - Rule evaluator (`RuleEngine`) for `<test>` checks.
  - Copilot client (`MsCopilotClient`) with endpoint mode + offline fallback.
  - Orchestrator (`QaCopilotOrchestrator`) as the runtime pipeline.
- `dotnet/src/QaAgent.AutoCAD`
  - `AutoCadQaAddin` to subscribe to document change events and push QA/copilot messages to a panel.
- `dotnet/src/QaAgent.MicroStation`
  - `MicroStationQaAddin` to subscribe to design-file change events and push QA/copilot messages to a dockable pane.
- `dotnet/QaAgent.sln`
  - Visual Studio solution containing all three projects.

### Copilot configuration

Set environment variables for live Copilot responses:

- `MS_COPILOT_ENDPOINT`
- `MS_COPILOT_API_KEY`

If unset, add-ons still run and emit offline QA guidance.

### Build (on a machine with .NET SDK)

```bash
dotnet build dotnet/QaAgent.sln
```

## Python prototype (kept)

- `src/qa_copilot/core/*` for XML loading/evaluation/orchestration.
- `src/qa_copilot/autocad/addon.py` and `src/qa_copilot/microstation/addon.py` for event-driven prototype adapters.

Run prototype demo:

```bash
PYTHONPATH=src python3 demo.py
```

Run test:

```bash
PYTHONPATH=src python3 -m pytest -q
```
