# Verification matrix — release 0.6.0, 2026-10-16

Final source **c651f5bad692a55f1fd36c27442e4aa5c2da1400**, run
[38763544008](https://github.com/kris70lesgo/aster-terminal/actions/runs/38763544008):
macOS/Linux **58 passed + 2 Windows-only skips**; Windows **51 passed + 9
Unix-only skips**; **60 total, zero failures** on each OS. All native packages
pass baseline, global hotkey summon/hide/show and abrupt UI crash/reopen; Unix
packages additionally pass native tmux. Versioned reports/images/TRX are in
`evidence/v0.6.0-{rid}`, with exact installed-archive 2× Retina checks in
`evidence/v0.6.0-macos-retina`. `ci-v0.6.0.json` and `payload-v0.6.0.json` record
source and package provenance.

| New acceptance | Evidence |
|---|---|
| Lazy dedicated terminal, hide/show retains actual shell, disable/close releases key and preserves normal layout | HotkeyDesktopTests, settings/preferences roundtrip |
| Real OS registration, another foreground process, injected global chord, summon/hide/show with retained variable, normal cleanup | hotkey/hotkey.json and native image on all three OSes; no physical-focus skips in final CI |
| Immediate queued-input/detach/reattach retains last actual shell value | HostedSessionTests, eight cycles per run; 15 focused local runs / 120 cycles |
| Late output cannot reach disposed detach subscription | Reproduced trace in detach-disposal-before-fix.txt, corrected cleanup order, full suite/native recovery |

Local macOS 26 verifies native registration/Carbon dispatch/unregister and the
exact installed package at 2× Retina. Its human physical-key attempt was not
detected before fixture expiry; local cross-app physical summon is **unverified**.
Wayland, custom global chords, per-profile dropdown animation, keyboard-layout /
sleep / multi-monitor and IME/native gesture coverage retain explicit gaps.
The fixed chord is opt-in. Python API and automatic tmux -CC remain unfinished.

## Historical release 0.5.0

### Verification matrix — release 0.5.0, 2026-10-15

Final source **96a90b4f3fdf45f78d108cc6b4dba5768285e103**, run
[38752301646](https://github.com/kris70lesgo/aster-terminal/actions/runs/38752301646):
macOS/Linux **55 passed + 2 Windows-only skips**; Windows **48 passed + 9
Unix-only skips**; all **57 total, zero failures**. All three final packages
pass native baseline and abrupt UI crash/reopen. Unix packages additionally
pass native tmux control attach. Versioned reports/images/TRX are under
`evidence/v0.5.0-{rid}`, with local installed Retina checks in
`evidence/v0.5.0-macos-retina`; provenance is in `ci-v0.5.0.json`.

| New acceptance | Evidence |
|---|---|
| Actual live shell variable/process identity survives detach; exclusive owner and rejected token | HostedSessionTests |
| Window detach/reopen recovers same pane; normal close terminates host/PTY | RecoveryDesktopTests |
| Native abrupt UI exit/reopen retains actual shell variable, metadata and Unix PID; input/resize/cleanup | recovery/recovery.json and before/after.png on all three OSes |
| Frame/grid bounds, journal compaction, cell pixel geometry and inline images | HostProtocolTests; baseline native graphics colours |
| Strict tmux layouts, raw octal/split-UTF8 bytes, independent panes, resize/new/close, Vim edit/return and live reattach | TmuxTests on macOS/Linux; explicit Windows skips |
| Real tmux native controls survive nested splits | TmuxDesktopTests |
| Native tmux split/input/Unicode/kernel resize; exact grid fits without clipping; same-shell reattach | tmux/tmux.json and captures on macOS/Linux, local 2× Retina |

Recovery bounds and text-checkpoint limits, and explicit local tmux `-C` scope,
are in architecture.md. Automatic in-shell `-CC`, remote/WSL attach, complete
checkpoint/DVR/graphics parity, Python API and global hotkeys remain unfinished.
Physical IME/AltGr, native clipboard/mouse gestures, multi-monitor, Windows 11
and Wayland retain explicit verification gaps. The failed Windows recovery and
native tmux candidates are preserved in migration-log.md / evidence.

## Verification matrix — release 0.4.0, 2026-10-15

The 0.3.0 baseline below remains covered. Version 0.4.0 adds profile output rules,
inline image pixel acceptance and anchored scrollback during incoming output.
Local full suite: **46 passed, 2 Windows-only skips, 0 failures / 48 total**.
The final extracted self-contained package passes native acceptance at 2× Retina
scaling on macOS 26.7.2/M5. Final run
[38700844528](https://github.com/kris70lesgo/aster-terminal/actions/runs/38700844528),
source `1cf4a149b9f6c17df2b9e7ad4e4a591dc3d61851`, passes all three targets:
macOS/Linux **46 passed + 2 Windows-only skips**, Windows **41 passed + 7
Unix-only skips**; all **48 total, zero failures**. Every final native package
also passes shell/input/resize/replay/rule/graphics/settings/render/cleanup.
Reports, images and TRX are in `evidence/v0.4.0-{rid}`, plus the installed local
Retina check in `evidence/v0.4.0-macos-retina`. `ci-v0.4.0.json` records provenance.
The first candidate's Windows cold-start rule failure is preserved in
`ci-first-trigger-v0.4.0.json` and migration-log.md.

| New acceptance | Evidence |
|---|---|
| Completed-line regex, fragmented/wrapped Unicode, rewritten text, cell highlighting | TriggerTests |
| Bookmark navigation after reflow; bounded records; eviction/reset/disable | TriggerTests |
| Profile rule validation, persistence, save/cancel; real shell and history UI | TriggerDesktopTests / SettingsStore |
| Sixel / OSC1337 PNG / direct-data kitty RGBA through adapter | GraphicsTests: fragmented bytes plus actual Skia red pixels, reset |
| Malformed/oversize graphics and rejected kitty file transfer | GraphicsTests: no image, continued text |
| Scrolled content survives new output until eviction; history clear | EngineTests |
| Native real-shell rule, three image colours, profile Triggers page | native-smoke.json / triggers.png / graphics.png / preferences-triggers.png |

Rule bounds and unsupported actions are in [output-rules.md](output-rules.md).
Graphics variants/animation, IME, physical native clipboard/mouse gestures,
multi-monitor, Windows 11 and Wayland retain explicit gaps. Native tmux -CC, Python API and global hotkeys remain unimplemented.
Live reconnection is under verification above.

## Historical release 0.3.0, 2026-10-15

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
| Physical native key input | 0.3.0 per-pid Cmd+T, pwd, replay open verified | Not manually verified | Not manually verified |
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

Final release-code run [38693452893](https://github.com/kris70lesgo/aster-terminal/actions/runs/38693452893),
build commit `e3a0a02d4c07bfc6e0c1429b4e2514b777f441e8`:
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
The final code passes local native acceptance on this Mac at 2× Retina scaling,
including historical replay while shell output continues, viewer resizing,
compact preferences, native menu configuration and cleanup. Physical per-pid
Cmd+T creates exactly one tab, p/w/d/Return executes pwd, and the replay shortcut
opens the viewer. Physical replay seek/close injection was inconclusive; native
menu clicks, IME and multi-monitor coverage remain manual gaps. Local isolated
captures and reports are in `evidence/v0.3.0-macos-retina`.

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
0.3.0 evidence for historical 0.3.0 claims. See [performance.md](performance.md)
for workload and measurement limits.

The earlier 1× candidate passed hosted acceptance but failed local 2× replay.
The fixed-grid viewer prevents layout reflow from mutating history. The failing
report is retained as `evidence/retina-before-fix.json`; the earlier CI record is
`evidence/ci-before-retina-v0.3.0.json`. Final three-OS reports supersede those
candidate results. The PowerShell fixture waits for its actual prompt before input.
