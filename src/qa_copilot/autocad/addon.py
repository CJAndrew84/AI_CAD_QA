from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

from qa_copilot.core.models import DocumentSnapshot
from qa_copilot.core.orchestrator import QaCopilotOrchestrator


@dataclass(slots=True)
class AutoCADEvent:
    event_name: str
    drawing_path: str
    file_format: str = "dwg"


class AutoCADQaCopilotAddOn:
    """AutoCAD plug-in orchestration logic.

    Hook `on_document_changed` from Autodesk AutoCAD .NET events and render returned
    advice in a PaletteSet / docked panel.
    """

    def __init__(self, repo_root: Path) -> None:
        self.orchestrator = QaCopilotOrchestrator(
            master_xml=repo_root / "Rule XML Masters" / "Master.xml",
            rules_dir=repo_root / "Rule XML Files",
        )

    def on_document_changed(self, event: AutoCADEvent) -> str:
        snapshot = DocumentSnapshot(
            platform="AutoCAD",
            file_path=event.drawing_path,
            file_format=event.file_format,
        )
        result = self.orchestrator.run(snapshot)
        failed = [e for e in result.evaluations if not e.passed]
        if not failed:
            return "✅ AutoCAD QA: all checks passed."

        actions = "\n".join(f"- {a}" for a in result.advice.actions)
        return f"⚠️ AutoCAD QA found {len(failed)} issue(s).\n{result.advice.summary}\n{actions}"
