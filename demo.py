from pathlib import Path

from qa_copilot.autocad.addon import AutoCADQaCopilotAddOn, AutoCADEvent
from qa_copilot.microstation.addon import MicroStationQaCopilotAddOn, MicroStationEvent

root = Path(__file__).parent

acad = AutoCADQaCopilotAddOn(root)
print(acad.on_document_changed(AutoCADEvent(event_name="save", drawing_path="/tmp/sample.dwg")))

ms = MicroStationQaCopilotAddOn(root)
print(ms.on_design_file_changed(MicroStationEvent(event_name="save", dgn_path="/tmp/sample.dgn")))
