from __future__ import annotations

from .models import DocumentSnapshot, RuleCheck, RuleDefinition, RuleEvaluation


class RuleEngine:
    def evaluate(self, snapshot: DocumentSnapshot, rules: list[RuleDefinition]) -> list[RuleEvaluation]:
        evaluations: list[RuleEvaluation] = []
        for rule in rules:
            if not rule.checks:
                evaluations.append(
                    RuleEvaluation(
                        rule_id=rule.rule_id,
                        passed=True,
                        severity=rule.severity,
                        message="No executable checks found; informational only.",
                        source_file=rule.source_file,
                    )
                )
                continue

            rule_passed = True
            message = "All checks passed"
            for check in rule.checks:
                passed = self._evaluate_check(snapshot, check)
                if not passed:
                    rule_passed = False
                    message = check.false_message or f"Failed check: {check.left} {check.op} {check.right}"
                    break
                message = check.true_message or message

            evaluations.append(
                RuleEvaluation(
                    rule_id=rule.rule_id,
                    passed=rule_passed,
                    severity=rule.severity,
                    message=message,
                    source_file=rule.source_file,
                )
            )
        return evaluations

    def _evaluate_check(self, snapshot: DocumentSnapshot, check: RuleCheck) -> bool:
        left = self._resolve_token(check.left, snapshot)
        right = self._resolve_token(check.right, snapshot)
        op = check.op.strip().lower()

        if op in ("=", "i="):
            return str(left).lower() == str(right).lower()
        if op == "!=":
            return str(left).lower() != str(right).lower()
        if op in {"contains", "in"}:
            return str(right).lower() in str(left).lower()
        return False

    @staticmethod
    def _resolve_token(token: str, snapshot: DocumentSnapshot):
        token = token.strip()
        if token.startswith("$") and token.endswith("$"):
            key = token.strip("$").lower()
            mapping = {
                "file_format": snapshot.file_format,
                "file": snapshot.file_path,
                "model": snapshot.model_name,
                "units": snapshot.units,
            }
            return mapping.get(key, snapshot.attributes.get(key, ""))
        return token
