# Migration report — competition release 0.9.0

Aster Terminal delivers the competition subset of an iTerm2 behavior migration:
real local terminals, profiles, tabs, nested panes, search, themes, persisted
configuration/layouts, original opt-in shell integration, bounded text-screen
replay, safe profile output rules, live local shell recovery and explicit native
Unix tmux control attach. Full upstream parity
remains Milestone B. The README contains the mandatory public write-up.

1. **Difficulty/completeness:** iTerm2 combines Objective-C/Swift VT state,
   AppKit/Metal drawing and persistent native process servers. Aster rewrites
   desktop/session boundaries, directly adapts the OSC133 exit-code scan and
   OSC1337 metadata, and bundles original bash/zsh integration. It reuses the
   MIT Devolutions parser/render/control rather than pretending that those are
   translations of iTerm2's Metal code. The parity matrix records advanced gaps.
2. **Application quality:** genuine shells, independent tabs/panes, close
   confirmation, focus/zoom, save scrollback, find, copy/paste, profiles, actual
   font/colour updates and dark/light/system themes. Preferences are transactional;
   malformed settings recover with preserved originals. Native rendered snapshots
   cover initial shell, tabs, splits, search, compact settings and replay on
   three OSes in 0.5.0 (final combined provenance in test-matrix.md). Local macOS acceptance also
   passes at 2× Retina scaling after repairing historical viewer reflow. The interface removes permanent chrome and exports
   macOS commands to the system menu. Screen replay uses bounded portable
   keyframes and an independent renderer; the live shell keeps running. Profile
   rules add matched-cell highlights/bookmarks with bounded history navigation.
   Sixel, OSC1337 PNG and direct-data kitty RGBA have adapter and native pixel
   acceptance, while broader graphics compatibility remains unverified.
3. **Cross-platform implementation:** one Avalonia/C# host; ConPTY for Windows
   cmd/PowerShell and a compiled MIT forkpty helper on macOS/Linux. All targeted
   packages launch and exercise their native backend on their own OS in CI.
   Linux is verified through X11/Xvfb; physical Windows 11/Wayland/multi-monitor
   coverage is not substituted by hosted tests. See test-matrix.md.
4. **Engineering quality:** pinned SDK/packages/upstream commits, lockfiles,
   atomic persistence, validation, bounded queues/history/control strings,
   serialized layout changes, real child cleanup and final-binary smoke tests.
   Linux cleanup handles background process groups in the PTY's own session.
   Terminal controls detach and drain pending UI rendering before async disposal.
   Current-user authenticated hosts preserve real shells across frontend loss;
   accepted PTY input uses host lifetime to avoid killing ConPTY on detach.
   Bounded tmux control framing preserves raw bytes between UTF-8 fragments;
   server-owned grids and pixel sizing prevent fullscreen pane clipping.
   Test regressions exercise failures as well as success. Performance numbers
   describe actual workloads and expose the expense of full-history snapshots.
5. **Usefulness/costs:** migration-log.md records concrete failures/repairs.
   Human hours and AI credit costs were not tracked and remain unmeasured.
   Public GPL source/build scripts and full dependency notices accompany binaries.
   Upstream GPL mismatch is resolved from the explicit COPYING grant; exact
   dependency sources and native licence alternatives are documented separately.

Milestone A release deliverables: source, runnable three-platform archives with
checksums, before/after evidence, tests and write-up. Entrant declarations and
contest form submission remain separate human steps; no submission is asserted.
Milestone B includes automatic in-shell tmux -CC / remote attach, full binary/image DVR compatibility,
full trigger action/plist compatibility, full Python API parity, configurable hotkeys/Wayland and broader graphics/compatibility coverage.

0.6.0 adds an opt-in native global summon window: registered Carbon / Win32 /
XCB chord, dedicated real terminal, retained shell through hide/show, visible
conflicts and release on disable/exit. Final source c651f5b / run 38763544008
passes all three native packages including another-process foreground injection.
Local Retina package checks pass; the human chord attempt on macOS 26 remains
unverified. Verification also found/fixed a detach-output disposal race that could
end the supposedly persistent shell. The failure and 120-cycle regression evidence
are retained in migration-log.md. No new runtime dependency was introduced.

0.7.0 adds a disabled-by-default, authenticated original Python API core, using
pinned protobuf/WebSocket libraries and a bundled transport-only launcher. The
original iterm2 2.30 client now creates real windows/tabs/panes, sends commands,
reads Unicode viewport contents and closes owned sessions on all three platforms.
Six focused scripting tests and the full 66-test suite pass. Final source ef6d642 /
run 38792785139, payload/license audit and exact installed 2× Retina evidence are
recorded in test-matrix.md. The local test helper's virtualenv-path failure is
preserved separately. Unsupported APIs and historical coordinate limits remain
explicit in scripting.md; this is not a full AppleScript/iTerm2 API port.

0.8.0 adds bounded background Find. It adapts the pinned MIT Devolutions
NFC/grapheme/Unicode-word mapping, with original notices, and adds .NET regex,
logical anchors, cancellation and a compact native bar. Full-history UI matching
was replaced by bounded snapshot/matcher work off the dispatcher. Review found
and fixed a stale-revision race during incoming output; the regression and final
75-test/native three-OS run 38851975830 pass. Installed exact CI Mac archives pass
at 2× Retina. Resource/dialect/selection limits remain explicit in find.md, and
local measured allocation/snapshot costs are reported without a performance
parity claim. No new dependency was introduced.

In 0.8.0, one exact installed Mac scripting attempt completed API/PTY assertions but
remained in Avalonia's native event loop past the process-exit deadline. Three
fresh unchanged-archive scripting repeats exited cleanly. Two ordered local
batches timed out on scripting exit; independent passing checks are combined
with explicit process IDs/provenance, not represented as one passing batch. Cause is unresolved, with the failed exit and managed
stack retained. Hosted package exits passed on all three OSes; this does not
establish unconditional local macOS shutdown reliability.

0.8.1 reproduces and addresses the exit stall with a deferred process-local
AppKit stop/event bridge. Phase logs showed API/PTY cleanup and desktop.Shutdown
returning before the native loop stalled. A periodic-only approach failed and
was discarded. The final bridge leaves normal window/session cleanup intact;
it uses no forced exit or HID input. This follows AppKit's documented stop/event
contract and the wakeup pattern studied in pinned Avalonia's platformthreading.mm.
No framework fork or new NuGet dependency was introduced; existing MIT framework
notices and the new GPL source/build target accompany the app.

A separate transport regression held a desktop action incomplete while the
server stopped; cancellation now releases the listener and credential lease
without waiting for the UI continuation. Final 76-test/native three-OS run
38901660170 passes. Exact installed 2× Retina checks pass as one full ordered
batch, with three original-client repetitions and verified process/main-loop
exits. Native tmux acceptance now waits for kernel PTY size before its strict
shell check. The verifier refuses reused report roots. Old failures and the
rejected approach remain in lifecycle-investigation-v0.8.1; final archive/native
bridge/signature/notice provenance is in payload-v0.8.1.json.

0.9.0 shares the existing bounded Find bar with explicit native tmux panes.
Normal and tmux views now use the same regex/case/word/NFC Unicode/navigation
workflow, while each pane owns its cancellation/results. Metadata refresh
preserves the textbox’s focus and content-size changes resize tmux PTYs to fit
the bar. Creating a tmux window selects its new tab. Two real-tmux desktop
regressions and final Unix native acceptance pass in source 78376ee / run
38906845116; all three platforms pass 78 tests including platform skips and
native baseline/hotkey/recovery/original-Python checks. No dependency was added.
Tmux Find searches retained output since attach, not the server’s complete
pre-existing history. Local installed verification and its display limitations
are recorded separately in test-matrix.md.

The installed 0.9.0 follow-up passes on macOS 26.7.2 at 2× Retina after display
wake: eight fixture processes exit and three original Python runs record clean
native-loop return. Fresh reports/captures are in v0.9.0-macos-retina. The earlier
zero-display failure remains retained. No source or archive changed for this
verification update.

Final 0.10.0 source 87184b04c8c69a10ebc5a94647293b3fe3501732 / run
39001628564 passes native acceptance on all three targets: macOS/Linux
78 passed + 2 skips; Windows 69 passed + 11 skips; 80 total, zero failures.
Tab drag/move preserves live shells, splits/zoom, Find focus and saved/scripting
order; indexed API insertion is covered. Native captures were inspected on
Windows/Linux/macOS. The failed first Linux candidate is retained.

All three exact archive checksums, inventories, application version, required
notices, runtime/dependency payload and private/compiler/test exclusions pass.
Mac signatures, system-only bridge links/minimum target and installed/native
byte identity pass. The exact installed archive also passes an ordered eight-PID
batch at 2× Retina on macOS 26.7.2, with three clean scripting main-loop exits;
a normal installed launch has an on-screen window. Evidence: ci-v0.10.0.json,
payload-v0.10.0.json and v0.10.0-{rid}/v0.10.0-macos-retina. Full upstream parity
and wider physical-input/desktop coverage remain explicitly tracked.

## 2026-10-17 — final 0.11.0 UI acceptance

Final UI source **e283c832610ba307e9861310dee86c3b289f24fe**, run
[39003323494](https://github.com/kris70lesgo/aster-terminal/actions/runs/39003323494):
macOS/Linux **80 passed + 2 Windows-only skips**; Windows **71 passed + 11
Unix-only skips**; **82 total, zero failures** on each target. All three final
native packages pass baseline, global hotkey, crash/reopen and the original
Python client with clean scripting main-loop return. Unix also passes native tmux.

The exact installed Mac archive passes the full ordered verifier at **2× Retina
on macOS 26.7.2**: all eight fixture processes exited, including three unchanged
original Python client runs with matching clean native-loop return. A separate
normal installed launch created an on-screen window; only window metadata was
recorded, with no input or screenshot of the user shell. The first installed
attempt had a Python pre-connection timeout; its parent exited cleanly with code
1. The cause is unconfirmed. The failed attempt and successful instrumented
diagnostic remain in `ui-alignment-investigation-v0.11.0`; the full subsequent
unchanged pass remains in `v0.11.0-macos-retina`.

The invalid inactive-selector candidate was corrected before publication; the
first failed TRX remains alongside the passing local 82-test result. No new
dependency, VT host action or session ownership change was introduced. All three
payload inventories, notices, versions and checksums pass; the extracted and
installed Mac assembly/native bridge match and signatures verify. Native
terminal/Settings captures on all three platforms were inspected. Evidence:
`ci-v0.11.0.json`, `payload-v0.11.0.json`, `v0.11.0-{rid}` and
`v0.11.0-macos-retina`. The read-only caption accessibility record is explicitly
from the first passing baseline, not the final verifier PID. Full UI/feature
parity and physical OS interaction gaps remain in visual-parity.md and the
feature matrix. No contest submission is claimed.

## 2026-10-17 — final installed-reference UI release 0.12.0

Final source **e4e1646e98315136502b98866ac385f911de8827**, native CI
[39009937189](https://github.com/kris70lesgo/aster-terminal/actions/runs/39009937189):
macOS/Linux **83 passed + 2 Windows-only skips**, Windows **74 passed + 11
Unix-only skips**; **85 total, zero failures** on each target. All three final
packages pass baseline, global hotkey, crash/reopen and original Python-client
acceptance; Unix also passes native tmux with exact kernel-grid checks.

The exact installed Mac archive passes the full ordered verifier at **2× Retina
on macOS 26.7.2**: all eight fixture processes exited, including three original
Python runs with matching clean native-loop return. A separate normal window
was inspected at the reference's 587×462 logical size / 1174×924 captured pixels.
The default profile now uses Monaco 12, with backup; command/environment and
saved startup layout are retained. Command-comma is unsupported by the driver,
so the preset was applied atomically to saved settings. One initial fresh
comparison instance was closed through real caption/confirmation clicks; older
user windows were left running. Private shell screenshots remain unpublished.

All three archive hashes/inventories, notices, runtime/app versions and compiler/
credential exclusions pass. Mac signatures verify; installed assembly and native
exit bridge match the extracted CI archive. Evidence: ci-v0.12.0.json,
payload-v0.12.0.json, v0.12.0-{rid} and v0.12.0-macos-retina. The first CI
numbering failures and repairs remain in installed-reference-v0.12.0. No dependency
or upstream asset was introduced. OS font/compositing/menu differences and full
upstream UI/features remain outside this acceptance; no contest submission.
