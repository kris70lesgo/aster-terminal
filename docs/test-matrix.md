# Verification matrix — release 0.12.0, 2026-10-17

Final source **e4e1646e98315136502b98866ac385f911de8827**, native CI
[39009937189](https://github.com/kris70lesgo/aster-terminal/actions/runs/39009937189):
macOS/Linux **83 passed + 2 Windows-only skips**, Windows **74 passed + 11
Unix-only skips**; **85 total, zero failures** on each target. All three final
packages pass baseline, global hotkey, crash/reopen and original Python-client
acceptance; Unix also passes native tmux with exact kernel-grid checks.

| New acceptance | Evidence |
|---|---|
| Single-tab plus hidden; real window-number shortcuts; multi-tab insertion/overflow retained | ChromeDesktopTests and all final native baselines |
| Numbering survives hidden/reopened windows and separate application lifetimes | Corrected application-scoped weak registry, hide/show/navigation regression; first failed Mac/Windows TRX/log retained |
| Real history thumb drag, top/bottom sync, alternate-screen disabling, independent shell lifetime | NativeScrollbar real-shell desktop regression on all targets |
| Transactional reference preset preserves command/environment; 12 nominal points produce expected cell metrics | ReferenceAppearancePreset regression, settings save/cancel tests |
| Caption/history gutter remain usable at 587×462; tmux includes per-pane gutter in exact PTY size | reference-window.png on each native target; final Unix tmux.json |

The exact installed Mac archive passes the full ordered verifier at **2× Retina
on macOS 26.7.2**: all eight fixture processes exited, including three original
Python runs with matching clean native-loop return. A separate normal window
was inspected at the reference's 587×462 logical size / 1174×924 captured pixels.
The default profile now uses Monaco 12, with backup; command/environment and
saved startup layout are retained. Command-comma is unsupported by the driver,
so the preset was applied atomically to saved settings. One initial fresh
comparison instance was closed through real caption/confirmation clicks; older
user windows were left running. Private shell screenshots remain unpublished.

Evidence: `ci-v0.12.0.json`, `payload-v0.12.0.json`, `v0.12.0-{rid}` and
`v0.12.0-macos-retina`. `installed-reference-v0.12.0` preserves source-reference
measurements, failed first CI evidence, passing corrected local TRX and the first
local installed candidate's scoped report. Superseded runs 39009681029 and
39009839002 were cancelled; final claims use source/run above.

Physical window drag/snap, broader DPI/monitor/IME/clipboard matrices, the local
human Mac hotkey and complete upstream settings/feature parity remain gaps.
The font fallback and Windows/Linux in-window menu placement differ from the
Mac reference. Shell-generated startup text is not reconstructed as UI. See
[visual parity](visual-parity.md) for measured correspondence and limitations.

## Historical release 0.11.0

### Verification matrix — release 0.11.0, 2026-10-17

Final UI source **e283c832610ba307e9861310dee86c3b289f24fe**, run
[39003323494](https://github.com/kris70lesgo/aster-terminal/actions/runs/39003323494):
macOS/Linux **80 passed + 2 Windows-only skips**; Windows **71 passed + 11
Unix-only skips**; **82 total, zero failures** on each target. All three final
native packages pass baseline, global hotkey, crash/reopen and the original
Python client with clean scripting main-loop return. Unix also passes native tmux.

Reports/TRX/native captures: `evidence/v0.11.0-{rid}`. Source/run provenance:
`ci-v0.11.0.json`. Final archive inventories, checksums, versions, notices,
signatures and installed-byte audit: `payload-v0.11.0.json`.

| New acceptance | Evidence |
|---|---|
| Actual caption new/minimize/maximize/close, independent real shells and disposal | ChromeDesktopTests; all three target TRX files |
| Shared 32-point caption, three tabs fill 620 width; overflow reveals selected tab without stealing focus | ChromeDesktopTests; native-smoke.json and tab captures |
| Settings categories and profile segments navigate and retain focus at 760×540 minimum | ChromeDesktopTests; native preferences-compact.png |
| Tab movement, Find and same-session shell state survive new chrome | Existing desktop regressions and all native baselines |
| Native tmux kernel grid follows selected content/Find height | Unix desktop regression and native tmux.json |

The exact installed Mac archive passes the full ordered verifier at **2× Retina
on macOS 26.7.2**: all eight fixture processes exited, including three unchanged
original Python client runs with matching clean native-loop return. A separate
normal installed launch created an on-screen window; only window metadata was
recorded, with no input or screenshot of the user shell. The first installed
attempt had a Python pre-connection timeout; its parent exited cleanly with code
1. The cause is unconfirmed. The failed attempt and successful instrumented
diagnostic remain in `ui-alignment-investigation-v0.11.0`; the full subsequent
unchanged pass remains in `v0.11.0-macos-retina`.

Native captures were inspected on all three targets. These checks establish the
implemented UI subset, not pixel identity with upstream. OS window rendering,
font availability, physical drag/snap/IME/AltGr/clipboard, broader desktops and
Wayland/multi-monitor coverage, the human local Mac hotkey and unimplemented
settings pages remain gaps. See [visual parity](visual-parity.md).

## Historical release 0.10.0

### Verification matrix — release 0.10.0, 2026-10-17

Final source **87184b04c8c69a10ebc5a94647293b3fe3501732**, run
[39001628564](https://github.com/kris70lesgo/aster-terminal/actions/runs/39001628564):
macOS/Linux **78 passed + 2 Windows-only skips**; Windows **69 passed + 11
Unix-only skips**; **80 total, zero failures** on each target. All three final
native packages pass baseline, global hotkey, crash/reopen and original Python
acceptance, with clean scripting main-loop return. Unix also passes native tmux.
Reports/TRX/images are in `evidence/v0.10.0-{rid}`; source provenance is in
`ci-v0.10.0.json`, archive/notice/signature audit in `payload-v0.10.0.json`.

| New acceptance | Evidence |
|---|---|
| Pointer drags in both directions, click threshold, insertion markers, Escape/outside cancellation and close-button exclusion | TabReorderDesktopTests; all three target TRX files |
| Keyboard left/right wrapping preserves process/session IDs, shell variable, active split, zoom, Find focus/results, scripting and saved layout order | Real-shell desktop regression; native-smoke.json and tabs-reordered.png on each target |
| Original-wire indexed tab insertion retains requested order and previous selection | ScriptingDesktopTests |
| Ordinary tmux reaches exact inner PTY kernel size before shell stty; new-pane Find query finishes before isolation assertion | Corrected Linux/Unix tests; first candidate failure retained in tab-reorder-investigation-v0.10.0 |

The exact installed Mac archive passes the full ordered native verifier at
**2× Retina on macOS 26.7.2**, including three original Python repetitions.
All eight fixture processes exited, and each scripting journal contains its
matching PID and clean native-loop return. A normal installed launch also
creates an on-screen window. Only window metadata is retained for that normal
launch; no user-shell input or screenshot. Evidence: `v0.10.0-macos-retina`.

Native captures were inspected on all three targets. Pointer acceptance uses
routed headless input; native package acceptance routes keyboard commands.
Physical drag/IME/AltGr/gestures, broader desktops/Wayland/multi-monitor,
the local human Mac global chord and full upstream parity remain separate gaps.
Pane dragging, grouped/pinned/cross-window tabs and tmux server ordering are not
claimed. The About dialog and package names now derive the project version.

## Historical release 0.9.0

### Verification matrix — release 0.9.0, 2026-10-16

Final source **78376eec31ba30d01e3c31c91fd0ac56fafab958**, run
[38906845116](https://github.com/kris70lesgo/aster-terminal/actions/runs/38906845116):
macOS/Linux **76 passed + 2 Windows-only skips**; Windows **67 passed + 11
Unix-only skips**; **78 total, zero failures** on each target. All three final
native packages pass baseline, global hotkey, crash/reopen and the original
Python client, including clean native main-loop return. Unix also passes native
tmux with the new Find UI. Versioned reports/TRX/images are in
`evidence/v0.9.0-{rid}`; source provenance is in `ci-v0.9.0.json`.

| New acceptance | Evidence |
|---|---|
| Regex/case/whole-word/NFC Unicode, counts, Enter/Shift+Enter, invalid/latest query, pane/tab isolation and 620-width fit using actual tmux shells | TmuxFindDesktopTests.cs; final Unix TRX |
| Metadata refresh preserves Find textbox focus; created tmux window selects its new tab | Real-tmux desktop regression |
| Pane removal and detach cancel pending Find; tmux server shells survive | Real-tmux desktop regression; final Unix native tmux.json |
| Native bar options/navigation/errors/Unicode, compact screenshot, pane isolation, exact PTY grid fit while Find is open, detach/reattach retaining shell variable | tmux/tmux.json, tmux-find-compact.png and tmux-splits.png on final Mac/Linux packages |
| Shared bar retains normal-window behavior on all three OSes | FindDesktopTests; final native-smoke.json / find-compact.png |

The exact CI Mac archive is installed with a verified signature and matching
managed/native bytes. After the display was awakened, the complete ordered
installed verification passed on **macOS 26.7.2 at 2× Retina**, with **three
scripting repetitions**. All eight fixture processes exited; each scripting
run records matching PID and a clean native main-loop return. Reports/captures
and a successful CoreGraphics/CoreVideo probe are in `v0.9.0-macos-retina`.
Inspected compact Find captures fit and highlight the real Unicode output. A
subsequent normal installed-app launch creates an on-screen window; only window
metadata is retained, without capturing or typing into a user shell.

The earlier local baseline exited before its report while CoreGraphics returned
zero active displays and CoreVideo returned -6661. That failed verifier/probe
remain in `v0.9.0-macos-display-unavailable`; the later success does not erase
that failure. Native macOS 15 CI and installed macOS 26 checks are separate.

Tmux Find searches output retained since attach (including the hydrated initial
viewport), not the server's complete prior history. Local Windows tmux remains
unsupported; its new desktop fixtures explicitly skip there. Smart case/filtering,
automatic in-shell -CC/remote attach, physical local Mac hotkey, IME/AltGr/gestures,
Wayland/multi-monitor and wider API/graphics/DVR coverage remain tracked gaps.
`payload-v0.9.0.json` records archive, notice, signature and installed-byte checks.

## Historical release 0.8.1

### Verification matrix — release 0.8.1, 2026-10-16

Final source **d23eba2abfad621a8f3874cca4a9d98893538f98**, run
[38901660170](https://github.com/kris70lesgo/aster-terminal/actions/runs/38901660170):
macOS/Linux **74 passed + 2 Windows-only skips**; Windows **67 passed + 9
Unix-only skips**; **76 total, zero failures** on each target. All three final
native packages pass baseline, global hotkey, crash/reopen and the original
Python client. Unix also passes native tmux. All scripting reports include a
clean main-loop-return marker. Versioned reports/TRX/images are in
`evidence/v0.8.1-{rid}`; `ci-v0.8.1.json` records source provenance.

The exact extracted/installed CI Mac archive passes the full ordered native
batch at **2× Retina** on macOS 26.7.2, with **three scripting repetitions**.
`v0.8.1-macos-retina/verification.json` records matching PIDs and actual process
exits for every phase, including native-loop return for each scripting run.
This replaces the composite-only local scope of 0.8.0. Six earlier candidate
scripting runs also returned cleanly; they remain separate investigation evidence.

| New acceptance | Evidence |
|---|---|
| Stop listener/private credential lease while a desktop request remains unfinished; replacement can start without waiting for that UI continuation | ScriptingServerTests.StoppingWithAnInFlightDesktopRequestDoesNotWaitForItsUiContinuation; before-fix failure retained |
| Reproduced shutdown-return/native-loop stall; rejected periodic-only trial; deferred AppKit stop/event returns cleanly | lifecycle-investigation-v0.8.1; final scripting/lifecycle.log on native macOS and installed Retina Mac |
| Fresh report directory, matching process ID, actual process exit and clean native-loop return required | scripts/verify_installed_macos.py; installed verification.json |
| tmux metadata, actual owned-PTY kernel geometry and shell stty result agree after resize | tmux/tmux.json on final Unix packages and installed Mac; kernel sizes/wait recorded |
| New Mac bridge linked only to system libraries, deployment target 15.0, GPL corresponding source/notices, exact installed native/managed bytes and signatures | payload-v0.8.1.json |

Prior shell/VT/Find/replay/graphics/rules/profiles/recovery/API checks remain
covered. One earlier local test run paused a trigger on its bounded regex
wall-time policy during diagnostic/build activity. The unchanged focused trigger
suite and complete rerun pass; that failure is retained in investigation evidence,
without increasing safety budgets. No universal timing/reliability claim is made.

The native Mac fix handles the reproduced exit path; it is not full upstream
API or AppKit lifecycle parity. Local human physical hotkey input remains
unverified despite three-OS CI global-key passes. Wayland, IME/AltGr/native
gestures, broader graphics/DVR/API compatibility and other feature gaps remain
in the parity inventory. Payload checksums and notice inventory are in
`payload-v0.8.1.json`.

## Historical release 0.8.0

### Verification matrix — release 0.8.0, 2026-10-16

Final source **4d4f5b3ed477335e7d90f80badb09f7ef5ea8102**, run
[38851975830](https://github.com/kris70lesgo/aster-terminal/actions/runs/38851975830):
macOS/Linux **73 passed + 2 Windows-only skips**; Windows **66 passed + 9
Unix-only skips**; **75 total, zero failures** on each target. All three native
self-contained packages pass baseline, global hotkey, abrupt UI crash/reopen
and original Python client acceptance; Unix additionally passes native tmux.
Reports/images/TRX are in `evidence/v0.8.0-{rid}`. The exact installed CI Mac
archive passes independent checks at **2× Retina** in `v0.8.0-macos-retina`;
the local ordered-batch exit failure is qualified below.
`ci-v0.8.0.json` and `payload-v0.8.0.json` record source and archive provenance.

| New acceptance | Evidence |
|---|---|
| Soft wraps, NFC/combining/CJK/emoji cell mapping, regex/case/word/lookbehind, hard-line boundaries and zero-length handling | FindTests |
| Bounded capture preserves scroll; truncated prefixes, hit/query/time caps, malformed patterns/Unicode and cancellation | FindTests |
| Reflow/eviction/reset/buffer anchors; output after snapshot cannot be credited as searched by navigation | FindTests, including deterministic revision-race regression |
| Real shell output, bar shortcuts/navigation/options/errors, latest query, pane isolation and close cancellation | FindDesktopTests |
| Real shell regex/case/word/Unicode navigation and malformed-pattern feedback; compact 620-width native layout | native-smoke.json, search.png and find-compact.png on all three OSes and installed 2× Mac |
| Exact archives/checksums, signatures, executable modes, full notices and no user/test/compiler/credential payload | payload-v0.8.0.json |

Nine new focused tests extend the prior 66-test suite. Find uses bounded .NET
logical-line regex and leaves terminal selection independent; smart case,
filtering and the new controls in the explicit tmux window remain pending.
Details are in find.md. A separate local capture/matcher observation, including
allocation costs, is in performance.md. The earlier candidate passed CI but
review found a stale-revision race; final source includes the repair and regression.
No full upstream parity or physical local macOS hotkey pass is implied.

The first installed scripting attempt passed API/PTY checks but its process
remained alive beyond 70 seconds, idle in Avalonia's native macOS event loop.
Three fresh unchanged-archive scripting repeats exited cleanly. Two ordered
batches hit the exit timeout, so local evidence is a composite of independent
passing checks, not a clean complete-batch pass. `process-exit.json` identifies
the exact exited fixture processes. The initial failure and stack are
preserved in `scripting-exit-timeout-v0.8.0.json` / `.stack.txt`. Its cause remains
unresolved; no shutdown fix or unconditional local exit reliability is claimed.
Diagnostic-only builds also exposed one Python client timeout and one tmux
resize timeout; their retained failure reports are labelled separately and are
not counted as passing final-archive evidence.


## Historical release 0.7.0

### Verification matrix — release 0.7.0, 2026-10-16

Final source **ef6d64269f4825ae1b9947b8269f052bb1cb114c**, run
[38792785139](https://github.com/kris70lesgo/aster-terminal/actions/runs/38792785139):
macOS/Linux **64 passed + 2 Windows-only skips**; Windows **57 passed + 9
Unix-only skips**; **66 total, zero failures** on each target. All three native
self-contained packages pass baseline, global hotkey, abrupt UI crash/reopen
and original Python client acceptance; Unix additionally passes native tmux.
Reports/images/TRX are in `evidence/v0.7.0-{rid}`; exact installed Mac archive
passes at **2× Retina**, including Python 3.12.13, in `v0.7.0-macos-retina`.
`ci-v0.7.0.json` and `payload-v0.7.0.json` record source and archive provenance.

| New acceptance | Evidence |
|---|---|
| Private loopback cookie/Host/Origin/subprotocol, original protobuf IDs/notifications, malformed/oversize refusal, exclusive credential owner/restart | ScriptingServerTests |
| Actual tab/split/input/Unicode viewport/styles, scroll offset retained, profile listing and owned-session close | ScriptingDesktopTests over real authenticated WebSocket / PTY |
| Default off, preferences enable/idempotent update/disable/re-enable, rotated credentials and exit with a connected client | ScriptingPreferencesService test; settings/preferences persistence |
| Original iterm2 2.30 creates a separate native window and closes its only tab while original shell survives; tab/split, real shell-local variable + wide/combining Unicode read, layout refresh and close | scripting/python-client.json + scripting.json + python-panes.png on all three native packages and installed Retina Mac |
| Native remaining-window/PTY and credential cleanup | scripting.json; baseline/recovery/tmux/hotkey acceptance retained |
| ASP.NET Core 10.0.13 and Google.Protobuf 3.36.2 present with full notices; compiler tools/test payload/credentials absent | payload-v0.7.0.json, aspnet-runtime-provenance.json and source map |

CI uses Python 3.12.10 (macOS/Windows) and 3.12.15 (Linux); local original-client
checks also pass 3.14.7. Protobuf 7.36.2 / websockets 17.2 are pinned. The local
installed check initially timed out after its helper resolved the virtualenv
Python symlink to the base interpreter, bypassing the pinned environment. The
helper now retains the virtualenv path; the unchanged exact app archive passes
with Python 3.12.13 at 2×. Failure report is preserved in
`scripting-local-launcher-before-fix.json`; it is not counted as a pass.

Supported API operations and explicit incompatible RPC/history/coordinate limits
are in scripting.md. This establishes the core subset, not all iTerm2 scripting
or AppleScript parity. Physical local macOS 26 global-key input remains unverified;
final CI cross-application hotkeys pass. Wayland, IME/AltGr/native gestures,
multi-monitor and broader compatibility gaps remain in the feature inventory.

## Historical release 0.6.0

### Verification matrix — release 0.6.0, 2026-10-16

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
