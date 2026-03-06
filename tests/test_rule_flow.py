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
    # "SharedLists" is referenced first in Master.xml and its filename stem becomes the rule ID.
    shared_lists_evals = [
        evaluation
        for evaluation in result.evaluations
        if getattr(evaluation, "rule_id", None) == "SharedLists"
    ]

    # Verify that at least one SharedLists evaluation is present, indicating that
    # the rule was parsed from Master.xml and executed.
    assert shared_lists_evals, "Expected at least one evaluation for rule 'SharedLists'."

    # Verify the evaluation has the required attributes with correct types.
    shared_lists_result = shared_lists_evals[0]
    assert hasattr(shared_lists_result, "rule_id")
    assert isinstance(shared_lists_result.rule_id, str)
    assert shared_lists_result.rule_id == "SharedLists"
    assert hasattr(shared_lists_result, "passed")
    assert isinstance(shared_lists_result.passed, bool)
