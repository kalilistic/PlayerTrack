import json
import os

try:
    filepath = os.path.join(os.path.dirname(__file__), "..", "PlayerTrack.Plugin", "Resource", "Loc", "en.json")
    with open(filepath, 'r', encoding='utf-8') as f:
        data = json.load(f)

    sorted_data = {k: data[k] for k in sorted(data.keys())}

    with open(filepath, 'w', encoding='utf-8') as f:
        json.dump(sorted_data, f, ensure_ascii=False, indent=2, sort_keys=True)

except json.JSONDecodeError as e:
    print(f"JSON Decode Error: Check line {e.lineno} in the file.")

except Exception as e:
    print(f"An unexpected error occurred: {e}")
