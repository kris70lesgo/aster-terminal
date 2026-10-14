# Verification matrix — release 0.3.0, 2026-10-14

Local: Apple M5 arm64, macOS 26.7.2, SDK 10.0.402/runtime 10.0.13,
Avalonia 12.1.4. Hosted native evidence is separate from this workstation.

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
| Physical native key input | 0.2.0 macOS per-pid events executed pwd | Not manually verified | Not manually verified |
| IME/AltGr/native clipboard/mouse/DPI/multi-monitor/Wayland | Manual gaps | Manual gaps | Manual gaps |
| Live SSH and htop | Not tested | Not tested | Not tested |
| Bounded text screen replay / seek / live output / cleanup | PASS | PASS | PASS |
| iTerm2 -CC/binary DVR/API/reconnect/global hotkey | Not implemented | Not implemented | Not implemented |
| Engine throughput/search/resize/CPU/memory and native startup/grid resize | Measured | Measured in hosted report | Measured in hosted report |
| Public source, self-contained download/checksum/notices | Released | Released | Released |
| Developer ID notarization / Authenticode / package-manager integration | Not provided | Not provided | Portable archive only |

## Commands and evidence

```sh
dotnet restore Aster.slnx --locked-mode
dotnet build Aster.slnx -c Release --no-restore
dotnet test Aster.slnx -c Release --no-build --logger 'trx;LogFileName=acceptance.trx' --results-directory artifacts/tests
python3 scripts/package.py --rid osx-arm64 --smoke
# Linux: xvfb-run -a python3 scripts/package.py --rid linux-x64 --smoke
# Windows: python scripts/package.py --rid win-x64 --smoke
```

Final release-code run [38572021565](https://github.com/kris70lesgo/aster-terminal/actions/runs/38572021565),
build commit `66aac10d85d4e80845944bfccded96ac0385cd99`:
macOS **34 passed / 2 Windows-only skips**, Linux **34 passed / 2 Windows-only skips**,
Windows **29 passed / 7 Unix-only skips**, all **0 failures / 36 total**. Each
final self-contained package passed native window, real shell, routed text/Enter,
resize, historical replay while live output continues, themes, tabs/splits,
find, compact Settings render and window/session cleanup. Mac additionally
checks the native menu tree and hidden in-window menu; physical native menu
clicks/key-equivalent behavior still need manual coverage. Package preparation
verifies Mac deep/strict signatures before packaging and after archive extraction,
then reruns acceptance from the extracted application.

Versioned PNG/JSON/TRX evidence is in `docs/evidence/v0.3.0-{rid}`; CI provenance
is `ci-v0.3.0.json`. Images capture client surfaces of native windows, with
shell-generated acceptance text, and do not include the OS titlebar/menu bar.
Linux uses X11/Xvfb. Acceptance routes Avalonia input events; it does not
substitute for physical keyboard/IME/mouse tests.

Local final suite: **34 passed, 2 Windows-only skips, 0 failures, 36 total**.
The final local GUI launch is unavailable while macOS reports zero active
displays. Avalonia and an independent CoreVideo probe returned -6661 before
window creation. No display/session setting was changed. Earlier minimal UI-only
native captures passed locally; final replay/native support is established by
the hosted Mac package run, not those earlier captures. The installed update
must be reopened when a display is active.

Tests added for replay cover historical Unicode/colour/cursor/grid preservation,
eviction, oversize rejection, timestamp regression including a one-frame buffer,
disabling and close cleanup, and real PTY output while an independent viewer
seeks old frames. Existing fullscreen fixtures now route terminal responses and
wait for vim's initial screen / a command in the resized tmux pane before input
or exit. The first two candidate failures and repairs are in migration-log.md;
they are preserved as failures, not replaced with a compilation claim.

## Historical evidence

Release 0.2.0 run [38562058430](https://github.com/kris70lesgo/aster-terminal/actions/runs/38562058430),
build `6708f88905bafc9e7ed43e34b613cdd5a987fb63`: macOS/Linux 31 passed + 2 skips;
Windows 26 passed + 7 skips, all 0 failures / 33 total. Its evidence remains in
`release-macos` and `release-{rid}`. The first complete three-platform run
`38520129077` and first-slice captures remain accurately scoped. Use the versioned
0.3.0 evidence for current release claims. See [performance.md](performance.md)
for workload and measurement limits.
