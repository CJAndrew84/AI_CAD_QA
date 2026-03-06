from __future__ import annotations

from dataclasses import dataclass, field
from enum import Enum
from pathlib import Path
from typing import Any


class RuleSeverity(str, Enum):
    MANDATORY = "mandatory"
    ADVISORY = "advisory"


@dataclass(slots=True)
class RuleCheck:
    left: str
    op: str
    right: str
    true_message: str | None = None
    false_message: str | None = None


@dataclass(slots=True)
class RuleDefinition:
    rule_id: str
    name: str
    severity: RuleSeverity
    source_file: Path
    checks: list[RuleCheck] = field(default_factory=list)


@dataclass(slots=True)
class RuleEvaluation:
    rule_id: str
    passed: bool
    severity: RuleSeverity
    message: str
    source_file: Path


@dataclass(slots=True)
class DocumentSnapshot:
    platform: str
    file_path: str
    file_format: str
    units: str | None = None
    model_name: str | None = None
    attributes: dict[str, Any] = field(default_factory=dict)
    levels: list[str] = field(default_factory=list)
    references: list[str] = field(default_factory=list)
