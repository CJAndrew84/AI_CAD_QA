from pathlib import Path

from qa_copilot.core.models import DocumentSnapshot
from qa_copilot.core.orchestrator import QaCopilotOrchestrator


def test_rule_loading_and_evaluation():
    root = Path(__file__).resolve().parents[1]
    orchestrator = QaCopilotOrchestrator(
        master_xml=root / "Rule XML Masters" / "Master.xml",
        rules_dir=root / "Rule XML Files",
    )
    snapshot = DocumentSnapshot(platform="MicroStation", file_path="demo.dgn", file_format="dgn8")
    result = orchestrator.run(snapshot)

    assert len(result.evaluations) > 0
    assert result.advice.summary
