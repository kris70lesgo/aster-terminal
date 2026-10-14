# Verification matrix — release 0.2.0, 2026-10-14

Local host: Apple M5 arm64, macOS 26.7.2, SDK 10.0.402/runtime 10.0.13,
Avalonia 12.1.4. Hosted evidence is separate from this workstation.

| Area | macOS arm64 | Windows x64 | Linux x64 |
|---|---|---|---|
| VT/UTF-8/cursor/truecolour/alternate/paste/history/search | PASS | PASS | PASS |
| Real process command execution, resize, exit | PASS forkpty | PASS ConPTY cmd/PowerShell | PASS forkpty |
| Unix tty/stty/SIGINT and shell/background-child cleanup | PASS | OS-specific skip | PASS |
| Independent sessions, tabs/panes, close/window cleanup | PASS | PASS | PASS |
| vim alternate screen/edit/resize/return | PASS | Unix fixture skipped | PASS |
| Ordinary tmux and less, resize/return | PASS | Unix fixture skipped | PASS |
| Original bash/zsh integration, cwd/status/marks | PASS | Unix fixture skipped | PASS |
| UI input, clipboard/search, resize → process | PASS headless real PTY | PASS headless ConPTY | PASS headless real PTY |
| Nested panes/zoom/title/theme/font/layout reopen | PASS | PASS | PASS |
| Profile preferences save/cancel/validation; corrupt backup recovery | PASS | PASS | PASS |
| OSC8 associations and unsafe URI blocking | PASS | PASS | PASS |
| Final self-contained package native launch, routed input, resize, renders, cleanup | PASS local + hosted macOS 15 | PASS hosted Server 2025 | PASS Ubuntu 24.04 X11/Xvfb |
| Physical native key input | macOS per-pid events executed pwd | Not manually verified | Not manually verified |
| IME/AltGr/native clipboard/mouse/DPI/multi-monitor/Wayland | Manual gaps | Manual gaps | Manual gaps |
| Live SSH and htop | Not tested | Not tested | Not tested |
| iTerm2 -CC/DVR/API/reconnect/global hotkey | Not implemented | Not implemented | Not implemented |
| Engine throughput/search/resize/CPU/memory and native startup/grid resize | Measured | Measured in hosted report | Measured in hosted report |
| Public source, self-contained download/checksum/notices | Released | Released | Released |
| Developer ID notarization / Authenticode / package-manager integration | Not provided | Not provided | Portable archive only |

## Commands and evidence

```sh
dotnet restore Aster.slnx --locked-mode
dotnet build Aster.slnx -c Release --no-restore
dotnet test Aster.slnx -c Release --no-build --logger 'trx;LogFileName=acceptance.trx' --results-directory artifacts/tests
python3 scripts/package.py --rid osx-arm64 --smoke
# On Linux: xvfb-run -a python3 scripts/package.py --rid linux-x64 --smoke
# On Windows: python scripts/package.py --rid win-x64 --smoke
```

Final release-code hosted run: [38562058430](https://github.com/kris70lesgo/aster-terminal/actions/runs/38562058430), build commit `6708f88905bafc9e7ed43e34b613cdd5a987fb63`: macOS **31 passed / 2 Windows skips**, Linux **31 passed / 2 Windows skips**, Windows **26 passed / 7 Unix skips**, all **0 failures / 33 total**. Native package checks passed in every job, including Mac extraction/signature checks and Linux CJK fallback. Release tag adds documentation/evidence to the tested build source; binary build provenance is recorded in the release notes.

Local final suite: **31 passed, 2 Windows-specific skips, 0 failed, 33 total**.
The former renderer-disposal race was reproduced in the expanded suite, fixed,
and the nested close/reopen regression also passed on a separate rerun.

[First complete hosted three-platform run](https://github.com/kris70lesgo/aster-terminal/actions/runs/38520129077)
passed on all three OSes. Final release-code run and versioned TRX/native JSON
are in docs/evidence/release-{rid}. Each native report records OS/architecture,
checks and measurements; package creation fails if native acceptance fails.
The release assets are taken from successful jobs, not cross-compiled claims.
Mac packaging verifies deep/strict signatures before packaging and after archive
extraction, then runs acceptance from the extracted application.
Historical first-slice TRX/screenshots are preserved with their original scope.

Acceptance routes Avalonia events into actual terminal controls and processes;
it does not represent physical keyboard/IME testing. Native PNGs are rendered
from windows running their real OS backend, not UI mockups. Read
[performance.md](performance.md) for workload and measurement limits.
