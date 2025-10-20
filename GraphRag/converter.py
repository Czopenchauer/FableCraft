import json
import os
from pathlib import Path


def extract_mes_from_jsonl(input_file, output_dir="output"):
    """
    Read a JSONL file and extract 'mes' and 'tracker' fields from each JSON object.
    Save each to a separate JSON file in format { mes: mes, tracker: tracker }.

    Args:
        input_file (str): Path to the input JSONL file
        output_dir (str): Directory where output JSON files will be saved
    """
    # Create output directory if it doesn't exist
    Path(output_dir).mkdir(parents=True, exist_ok=True)

    mes_counter = 0

    print(f"Reading from: {input_file}")
    print(f"Output directory: {output_dir}")

    try:
        with open(input_file, 'r', encoding='utf-8') as f:
            for line_num, line in enumerate(f, 1):
                line = line.strip()

                # Skip empty lines
                if not line:
                    continue

                try:
                    # Parse JSON object
                    data = json.loads(line)

                    # Check if 'mes' field exists
                    if 'mes' in data:
                        mes_counter += 1

                        # Extract mes and tracker fields
                        extracted_data = {
                            "mes": data['mes'],
                            "tracker": data.get('tracker', {})
                        }

                        # Create output filename
                        output_filename = os.path.join(output_dir, f"message_{mes_counter:04d}.json")

                        # Write to JSON file
                        with open(output_filename, 'w', encoding='utf-8') as out_f:
                            json.dump(extracted_data, out_f, indent=2, ensure_ascii=False)

                        print(f"  [Line {line_num}] Extracted 'mes' and 'tracker' -> {output_filename}")
                    else:
                        print(f"  [Line {line_num}] No 'mes' field found, skipping")

                except json.JSONDecodeError as e:
                    print(f"  [Line {line_num}] Error parsing JSON: {e}")
                    continue

        print(f"\nCompleted! Extracted {mes_counter} messages to {mes_counter} JSON files.")

    except FileNotFoundError:
        print(f"Error: File '{input_file}' not found.")
    except Exception as e:
        print(f"Error: {e}")


if __name__ == "__main__":
    # Default input file
    input_file = "test/adventure.jsonl"

    # You can change the output directory here
    output_dir = "output"

    extract_mes_from_jsonl(input_file, output_dir)


