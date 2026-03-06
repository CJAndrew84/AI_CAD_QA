# PW_CAD_QA – AI CAD QA Agent

This repo includes:

- a **Python prototype** for XML QA evaluation + Copilot guidance, and
- **.NET add-on scaffolding** for AutoCAD and MicroStation hosts.

The goal is an embedded AI CAD QA agent that watches design activity and provides rule-compliance guidance from your XML QA rules.

## Existing QA rule sources

- Master: `Rule XML Masters/Master.xml`
- Referenced rule packs: `Rule XML Files/*.xml`

## .NET add-ons

### Projects

- `dotnet/src/QaAgent.Core`
  - `XmlRuleLoader`: loads `Master.xml` and referenced XML files.
  - `RuleEngine`: evaluates XML `<test>` checks.
  - `QaCopilotOrchestrator`: end-to-end rule evaluation + AI guidance pipeline.
  - `IAiAdvisor` + `AiAdvisorFactory`: pluggable AI back-end selection.
  - `MsCopilotClient`: Azure OpenAI-style Copilot endpoint client.
  - `Microsoft365AgentsSdkClient`: Microsoft 365 Agents-compatible client adapter.
- `dotnet/src/QaAgent.AutoCAD`
  - `AutoCadQaAddin`: subscribes to document change events and renders QA results.
- `dotnet/src/QaAgent.MicroStation`
  - `MicroStationQaAddin`: subscribes to design-file change events and renders QA results.
- `dotnet/QaAgent.sln`
  - Visual Studio solution containing all projects.

## Copilot / Microsoft 365 Agents configuration

1. Copy `.env.example` to `.env` and fill in values:

```bash
cp .env.example .env
```

2. Choose one runtime mode:

- **Microsoft 365 Agents mode (preferred when available):**
  - `M365_AGENTS_MESSAGES_ENDPOINT`
  - `M365_AGENTS_BEARER_TOKEN`
- **Azure OpenAI-style Copilot mode (fallback/default):**
  - `MS_COPILOT_ENDPOINT`
  - `MS_COPILOT_API_KEY`

`QaCopilotOrchestrator` uses `AiAdvisorFactory` to auto-select:

- Agents mode if both `M365_AGENTS_*` values are set.
- Otherwise, Copilot mode.
- If neither is configured, offline fallback guidance is returned.

## Build (on a machine with .NET SDK)

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
