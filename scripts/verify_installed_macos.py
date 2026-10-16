#!/usr/bin/env python3
"""Verify the installed native app without activating it; requires cua-driver.

Every run owns a fresh report directory. A successful JSON report is insufficient:
its process must exit, and scripting must also record a clean main-loop return.
"""
import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import subprocess
import sys
import time


def running(pid):
    try:
        os.kill(pid, 0)
        return True
    except ProcessLookupError:
        return False


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--python", type=Path, required=True)
    parser.add_argument("--mode", choices=["all", "scripting", "tmux"], default="all")
    parser.add_argument("--repeat-scripting", type=int, default=1)
    args = parser.parse_args()
    if sys.platform != "darwin":
        parser.error("This installed-app verifier requires macOS.")
    if not 1 <= args.repeat_scripting <= 20:
        parser.error("Use 1–20 scripting repetitions.")
    # Preserve a virtualenv's executable symlink: resolving it loses the environment.
    python = args.python.expanduser().absolute()
    if not python.is_file():
        parser.error("The Python environment executable does not exist.")
    root = Path(__file__).resolve().parents[1]
    base = args.report.expanduser().absolute()
    base.mkdir(parents=True, exist_ok=False)
    results = []

    def run(arguments, expected, abrupt=False):
        if expected.exists():
            raise RuntimeError("Refusing an existing report: " + str(expected))
        launched_at = time.time()
        start = time.monotonic()
        launch = subprocess.check_output([
            "cua-driver", "call", "launch_app", json.dumps({
                "name": "Aster Terminal", "creates_new_application_instance": True,
                "additional_arguments": arguments,
            }), "--compact"], text=True)
        instance = json.loads(launch)
        assert instance["bundle_id"] == "io.asterterminal.desktop"
        pid = instance["pid"]
        record = {"mode": arguments[0], "processId": pid, "passed": False}
        results.append(record)
        print("Checking", arguments[0], "PID", pid, flush=True)
        while not expected.exists() or running(pid):
            if time.monotonic() - start > 110:
                raise TimeoutError(f"Owned fixture PID {pid} did not produce a report and exit; see {expected.parent}")
            if not running(pid) and not expected.exists():
                raise RuntimeError(f"Owned fixture PID {pid} exited without its report")
            time.sleep(.1)
        assert expected.stat().st_mtime >= launched_at - 1, "Report predates launch"
        report = json.loads(expected.read_text())
        if "processId" in report:
            assert report["processId"] == pid, "Report belongs to a different process"
        if not abrupt:
            assert report["passed"], report.get("error")
        if arguments[0] == "--scripting-smoke":
            marker = f"pid={pid} main loop returned exitCode=0"
            assert marker in (expected.parent / "lifecycle.log").read_text(), "No clean main-loop return"
        record.update(passed=True, processExited=True, elapsedSeconds=time.monotonic() - start,
                      report=str(expected.relative_to(base)))
        print("Passed", arguments[0], flush=True)

    passed = False
    error = None
    try:
        if args.mode == "all":
            run(["--smoke-test", "--smoke-report", str(base / "baseline")], base / "baseline/native-smoke.json")
            recovery = str(base / "recovery")
            run(["--recovery-start", "--recovery-report", recovery], base / "recovery/before.json", abrupt=True)
            run(["--recovery-resume", "--recovery-report", recovery], base / "recovery/recovery.json")
        if args.mode in ("all", "tmux"):
            run(["--tmux-smoke", "--tmux-report", str(base / "tmux")], base / "tmux/tmux.json")
        if args.mode == "all":
            run(["--hotkey-smoke", "--hotkey-report", str(base / "hotkey")], base / "hotkey/hotkey.json")
        if args.mode in ("all", "scripting"):
            for index in range(args.repeat_scripting):
                folder = base / ("scripting" if index == 0 else f"scripting-{index + 1}")
                run(["--scripting-smoke", "--scripting-report", str(folder),
                     "--scripting-python", str(python), "--scripting-launcher", str(root / "scripts/python/aster_script.py"),
                     "--scripting-script", str(root / "tests/python/core_api.py")], folder / "scripting.json")
        passed = True
    except Exception as failure:
        error = str(failure)
        raise
    finally:
        (base / "verification.json").write_text(json.dumps({
            "passed": passed, "timestamp": datetime.now(timezone.utc).isoformat(),
            "scope": "Fresh installed-app fixtures; preserved foreground; actual process exits required",
            "results": results, "error": error,
        }, indent=2) + "\n")


if __name__ == "__main__":
    main()
