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

    # Basic sanity checks: we have some evaluations and non-empty advice.
    assert len(result.evaluations) > 0
    assert result.advice.summary

    # Stronger end-to-end checks: ensure a known rule from Master.xml is loaded
    # and that its evaluation produces a deterministic outcome for this snapshot.
    general_check_evals = [
        evaluation
        for evaluation in result.evaluations
        if getattr(evaluation, "rule_id", None) == "GeneralCheck"
    ]

    # Verify that at least one GeneralCheck evaluation is present, indicating that
    # the rule was parsed from Master.xml and executed.
    assert general_check_evals, "Expected at least one evaluation for rule 'GeneralCheck'."

    # For the demo.dgn DGN8 snapshot, GeneralCheck should deterministically pass.
    general_check_result = general_check_evals[0]
    assert (
        getattr(general_check_result, "passed", None) is True
    ), "Expected 'GeneralCheck' to pass for demo.dgn with file_format='dgn8'."
