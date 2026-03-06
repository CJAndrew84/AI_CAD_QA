from __future__ import annotations

from pathlib import Path
import xml.etree.ElementTree as ET

from .models import RuleCheck, RuleDefinition, RuleSeverity


class XmlRuleLoader:
    """Loads master QA XML + referenced XML rule files into normalized rule objects."""

    def __init__(self, master_xml: Path, rules_dir: Path) -> None:
        self.master_xml = master_xml
        self.rules_dir = rules_dir

    def load(self) -> list[RuleDefinition]:
        references = self._collect_references(self.master_xml)
        rules: list[RuleDefinition] = []
        for ref in references:
            source = self._resolve_reference(ref)
            if not source.exists():
                continue
            rules.extend(self._parse_rule_file(source))
        return rules

    def _collect_references(self, path: Path) -> list[str]:
        tree = ET.parse(path)
        root = tree.getroot()
        references: list[str] = []
        for rule in root.findall(".//rule"):
            reference = rule.findtext("reference")
            if reference:
                references.append(reference.strip())
        return references

    def _resolve_reference(self, name: str) -> Path:
        direct = self.rules_dir / name
        if direct.exists():
            return direct
        lower = {p.name.lower(): p for p in self.rules_dir.glob("*.xml")}
        return lower.get(name.lower(), direct)

    def _parse_rule_file(self, path: Path) -> list[RuleDefinition]:
        tree = ET.parse(path)
        root = tree.getroot()
        loaded: list[RuleDefinition] = []

        rule_nodes = [root] if root.tag == "rule" else []
        rule_nodes.extend(root.findall(".//rule"))

        for rule in rule_nodes:
            if rule.get("id") == "_init":
                continue
            severity = RuleSeverity(rule.get("type", "advisory").lower())
            name = rule.findtext("name") or rule.get("id", "unknown")
            checks = []
            for test in rule.findall(".//test"):
                checks.append(
                    RuleCheck(
                        left=test.attrib.get("l", ""),
                        op=test.attrib.get("op", "="),
                        right=test.attrib.get("r", ""),
                        true_message=rule.findtext(".//msg[@id='true']"),
                        false_message=rule.findtext(".//msg[@id='false']"),
                    )
                )
            loaded.append(
                RuleDefinition(
                    rule_id=rule.get("id", "unknown"),
                    name=name,
                    severity=severity,
                    source_file=path,
                    checks=checks,
                )
            )
        return loaded
