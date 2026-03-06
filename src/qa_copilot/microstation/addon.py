from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

from qa_copilot.core.models import DocumentSnapshot
from qa_copilot.core.orchestrator import QaCopilotOrchestrator


@dataclass(slots=True)
class MicroStationEvent:
    event_name: str
    dgn_path: str
    file_format: str = "dgn8"


class MicroStationQaCopilotAddOn:
    """MicroStation add-in orchestration logic.

    Hook `on_design_file_changed` from Bentley .NET APIs and display response in a
    dockable tool window.
    """

    def __init__(self, repo_root: Path) -> None:
        self.orchestrator = QaCopilotOrchestrator(
            master_xml=repo_root / "Rule XML Masters" / "Master.xml",
            rules_dir=repo_root / "Rule XML Files",
        )

    def on_design_file_changed(self, event: MicroStationEvent) -> str:
        snapshot = DocumentSnapshot(
            platform="MicroStation",
            file_path=event.dgn_path,
            file_format=event.file_format,
        )
        result = self.orchestrator.run(snapshot)
        failed = [e for e in result.evaluations if not e.passed]
        if not failed:
            return "✅ MicroStation QA: all checks passed."

        actions = "\n".join(f"- {a}" for a in result.advice.actions)
        return f"⚠️ MicroStation QA found {len(failed)} issue(s).\n{result.advice.summary}\n{actions}"
