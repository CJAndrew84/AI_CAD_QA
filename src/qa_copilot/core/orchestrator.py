from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

from .copilot import CopilotAdvice, MsCopilotClient
from .models import DocumentSnapshot, RuleEvaluation
from .rule_engine import RuleEngine
from .rule_loader import XmlRuleLoader


@dataclass(slots=True)
class QaResult:
    evaluations: list[RuleEvaluation]
    advice: CopilotAdvice


class QaCopilotOrchestrator:
    def __init__(self, master_xml: Path, rules_dir: Path, copilot: MsCopilotClient | None = None) -> None:
        self.loader = XmlRuleLoader(master_xml=master_xml, rules_dir=rules_dir)
        self.engine = RuleEngine()
        self.copilot = copilot or MsCopilotClient()

    def run(self, snapshot: DocumentSnapshot) -> QaResult:
        rules = self.loader.load()
        evaluations = self.engine.evaluate(snapshot, rules)
        advice = self.copilot.suggest(snapshot, evaluations)
        return QaResult(evaluations=evaluations, advice=advice)
