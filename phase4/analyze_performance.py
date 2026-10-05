"""Analyze HOPE guest-swap cadence. This is not displayed FPS or GPU timing."""
import argparse
import csv
import io
import json
import math
from pathlib import Path

FIELDS = {"frame", "elapsed_us", "frame_time_us", "gameplay_context", "renderer", "transition"}

def percentile(values, fraction):
    return values[max(0, math.ceil(len(values) * fraction) - 1)]

def summarize(path):
    lines = Path(path).read_text(encoding="utf-8-sig").splitlines()
    metadata = {}
    for line in lines:
        if line.startswith("#") and "=" in line:
            key, value = line[1:].strip().split("=", 1)
            metadata[key] = value
    if metadata.get("measurement") != "guest_swap_cadence_not_display_fps":
        raise ValueError("Not a HOPE guest-cadence capture; aggregate counter CSVs are not frame samples.")
    reader = csv.DictReader(io.StringIO("\n".join(line for line in lines if not line.startswith("#"))))
    if not reader.fieldnames or not FIELDS.issubset(reader.fieldnames):
        raise ValueError("Capture columns are missing.")
    groups = {}; transitions = 0; last_frame = last_elapsed = 0
    for row in reader:
        try:
            frame, elapsed, interval, context, transition = (int(row[key]) for key in
                ("frame", "elapsed_us", "frame_time_us", "gameplay_context", "transition"))
        except (ValueError, TypeError) as error:
            raise ValueError("Invalid capture row.") from error
        if frame <= last_frame or elapsed <= last_elapsed or interval <= 0 or interval > elapsed or context < 0 or transition not in (0, 1):
            raise ValueError("Capture contains invalid timing or sequence data.")
        if row["renderer"] not in ("Native", "Emulated"):
            raise ValueError("Unknown renderer.")
        last_frame, last_elapsed = frame, elapsed
        if transition:
            transitions += 1; continue
        state = "Gameplay" if context == 1 else "Menu/loading" if context == 0 else f"Context {context}"
        groups.setdefault((row["renderer"], state), []).append(interval / 1000)
    result = []
    for (renderer, state), values in sorted(groups.items()):
        values.sort(); total = sum(values); slow_count = max(1, math.ceil(len(values) * .01))
        slow_mean = sum(values[-slow_count:]) / slow_count
        result.append({"renderer": renderer, "state": state, "samples": len(values),
            "sampled_seconds": total / 1000, "mean_ms": total / len(values),
            "median_ms": percentile(values, .5), "p95_ms": percentile(values, .95),
            "p99_ms": percentile(values, .99), "worst_ms": values[-1],
            "cadence_hz": 1000 * len(values) / total,
            "slowest_1_percent_cadence_hz": 1000 / slow_mean,
            "intervals_over_33_33ms": sum(value > 1000/30 for value in values),
            "intervals_over_50ms": sum(value > 50 for value in values),
            "short_capture": len(values) < 300 or total < 10000})
    if not result:
        raise ValueError("Capture has no usable non-transition frame intervals.")
    return {"file": str(Path(path).resolve()), "measurement": metadata["measurement"],
        "metadata": metadata, "excluded_transition_intervals": transitions, "groups": result,
        "limits": "Guest swap cadence includes pacing/waits. It is not displayed FPS, CPU/GPU execution time, memory use, shader stalls or load duration. Menu/loading cannot be separated by this signal."}

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("capture", type=Path)
    parser.add_argument("--output", type=Path, help="Write JSON to a new report file")
    args = parser.parse_args()
    try:
        report = summarize(args.capture)
        text = json.dumps(report, indent=2, allow_nan=False)
        if args.output:
            with args.output.open("x", encoding="utf-8") as output: output.write(text + "\n")
        print(text)
    except (OSError, ValueError) as error:
        parser.exit(1, f"Capture analysis failed: {error}\n")

if __name__ == "__main__": main()
