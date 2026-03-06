# PW_CAD_QA – QA Copilot Add-on Scaffolding

This repository now includes a working **QA Copilot orchestration layer** that reads your XML QA rules and can be embedded into:

- **AutoCAD + AutoCAD verticals** (Civil 3D, Plant 3D, Map 3D, etc.)
- **MicroStation + Bentley verticals** (OpenRoads, OpenBuildings, OpenRail, etc.)

## What was built

- XML master/reference loader for the existing QA rule files.
- Rule evaluation engine for XML `<test>` expressions.
- Microsoft Copilot-compatible client (`MsCopilotClient`) that can call a Microsoft-hosted chat endpoint (Azure OpenAI-style) and return actionable remediation tips.
- AutoCAD add-on adapter class that can be wired to document/database change events.
- MicroStation add-on adapter class that can be wired to design file/model change events.

## Project layout

- `src/qa_copilot/core/`
  - `rule_loader.py`: reads `Rule XML Masters/Master.xml` and referenced XML files.
  - `rule_engine.py`: evaluates checks against a live `DocumentSnapshot`.
  - `copilot.py`: composes QA failures into a Copilot prompt and fetches advice.
  - `orchestrator.py`: single entry point used by both CAD platform adapters.
- `src/qa_copilot/autocad/addon.py`
  - AutoCAD adapter (`AutoCADQaCopilotAddOn`) and event model.
- `src/qa_copilot/microstation/addon.py`
  - MicroStation adapter (`MicroStationQaCopilotAddOn`) and event model.
- `demo.py`
  - Runs a local demonstration of both adapters.

## How to wire into real add-ins

## AutoCAD / Verticals

1. Create a .NET plug-in (or Python bridge) that subscribes to document/database events.
2. On events like save, object append/modify, xref attach, call `on_document_changed`.
3. Show returned advice in a docked palette.

## MicroStation / Verticals

1. Create a .NET add-in that subscribes to design file/model events.
2. On events like file save, reference attach, level changes, call `on_design_file_changed`.
3. Show returned advice in a dockable pane/task dialog.

## Configure Microsoft Copilot endpoint

Set environment variables:

- `MS_COPILOT_ENDPOINT`
- `MS_COPILOT_API_KEY`

If not set, the add-ons still run with offline/local fallback guidance.

## Quick run

```bash
PYTHONPATH=src python3 demo.py
```

