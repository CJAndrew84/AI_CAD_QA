from __future__ import annotations

import json
import os
from dataclasses import dataclass
from typing import Iterable
from urllib import request

from .models import DocumentSnapshot, RuleEvaluation


@dataclass(slots=True)
class CopilotAdvice:
    summary: str
    actions: list[str]


class MsCopilotClient:
    """Minimal client for Microsoft-hosted chat completions (Azure OpenAI style)."""

    def __init__(self, endpoint: str | None = None, api_key: str | None = None) -> None:
        self.endpoint = endpoint or os.getenv("MS_COPILOT_ENDPOINT", "")
        self.api_key = api_key or os.getenv("MS_COPILOT_API_KEY", "")

    def suggest(self, snapshot: DocumentSnapshot, evaluations: Iterable[RuleEvaluation]) -> CopilotAdvice:
        failed = [e for e in evaluations if not e.passed]
        if not self.endpoint or not self.api_key:
            return self._offline_advice(failed)

        payload = {
            "messages": [
                {
                    "role": "system",
                    "content": "You are an engineering CAD QA copilot. Give concise, actionable fixes.",
                },
                {
                    "role": "user",
                    "content": json.dumps(
                        {
                            "platform": snapshot.platform,
                            "file": snapshot.file_path,
                            "failed_rules": [
                                {
                                    "rule_id": r.rule_id,
                                    "severity": r.severity.value,
                                    "message": r.message,
                                }
                                for r in failed
                            ],
                        }
                    ),
                },
            ],
            "temperature": 0.2,
        }

        req = request.Request(
            self.endpoint,
            data=json.dumps(payload).encode("utf-8"),
            headers={
                "Content-Type": "application/json",
                "api-key": self.api_key,
            },
            method="POST",
        )
        try:
            with request.urlopen(req, timeout=15) as response:
                body = json.loads(response.read().decode("utf-8"))
            text = body["choices"][0]["message"]["content"]
            lines = [line.strip("- ") for line in text.splitlines() if line.strip()]
            return CopilotAdvice(summary=lines[0] if lines else "Review failed checks.", actions=lines[1:4])
        except Exception:
            # On network/HTTP/JSON/schema errors, fall back to offline advice
            return self._offline_advice(failed)
    @staticmethod
    def _offline_advice(failed: list[RuleEvaluation]) -> CopilotAdvice:
        if not failed:
            return CopilotAdvice(summary="All QA checks passed.", actions=["Proceed to publish."])
        actions = [f"{f.rule_id}: {f.message}" for f in failed[:3]]
        return CopilotAdvice(summary="Fix the failed QA rules before publish.", actions=actions)
