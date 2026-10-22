# Screenshot and runtime evidence

## Release candidate 0.14.0 — source 0d5ca38 / CI run 39748978754

`v0.14.0-osx-arm64`, `v0.14.0-win-x64` and `v0.14.0-linux-x64` contain final
123-case TRX results, native shell/window renders and native acceptance reports
from the exact source commit linked above. Each target passes its full test suite
with platform-only skips, a real packaged window/PTY run, the system-wide hotkey
summon/hide/resummon test, original Python-client control, and live session
recovery. macOS/Linux also pass native tmux control mode. Hosted captures are 1×;
the optional Mac-local 2× development capture is pre-release evidence and does
not establish full screenshot identity.

`ci-v0.14.0.json` records per-target counts and runtime reports. The exact three
release archives, SHA-256 values, notice inventory and extracted Mac signature
audit are in `payload-v0.14.0.json`. macOS is ad-hoc signed without Developer ID
notarization; Windows is unsigned. Private user shell captures are excluded.

## Historical release 0.9.0

`v0.9.0-{rid}` contains final 78-test/native package reports/images from source
78376eec31ba30d01e3c31c91fd0ac56fafab958 / run 38906845116. Unix adds the
shared native tmux Find bar, compact Unicode screenshot, exact grid fit and
detach cancellation; prior normal-window/recovery/graphics/API checks pass on
all three targets. `ci-v0.9.0.json` and `payload-v0.9.0.json` record provenance.

The exact CI Mac archive passes the full installed native sequence after display
wake on macOS 26.7.2 at 2× Retina, with eight exited fixture PIDs and three clean
original-Python/main-loop exits. `v0.9.0-macos-retina` contains the verification,
phase reports, fresh display probe and inspected captures. The earlier zero-display
failure remains in `v0.9.0-macos-display-unavailable`; it is not relabelled as a
pass. Native macOS 15 CI remains separately recorded. `tmux-find-investigation-v0.9.0` retains
the expected before-implementation regression; all commands/output belong to
uniquely owned fixture servers. No normal user shell was captured.

## Historical release 0.8.1

`v0.8.1-{rid}` contains final 76-test/native package reports/images from source
d23eba2abfad621a8f3874cca4a9d98893538f98 / run 38901660170. Native scripting
now records matching PID, Python progress, desktop Exit and main-loop return.
The exact installed CI Mac archive passes a full ordered 2× Retina batch and
three scripting repetitions in `v0.8.1-macos-retina`; verification.json records
actual exited PIDs. Kernel-size observations accompany Unix tmux acceptance.
`ci-v0.8.1.json` / `payload-v0.8.1.json` record provenance and the native bridge
system-library/deployment/signature/notice audit.

`lifecycle-investigation-v0.8.1` preserves the before-fix native-loop stall,
rejected periodic-only attempt, held-request regression, tmux one-shot-size
failure and trigger-budget pause. Candidate successes are explicitly separated
from final archives. A debugger inspection is not counted as a successful exit.
Physical local Mac keys, IME/gestures and multi-monitor behavior remain separate
coverage gaps. These client renders and routed native actions are not claims of
all physical interactions or full iTerm2 parity.

## Historical release 0.8.0

`v0.8.0-{rid}` contains the final 75-test/native package evidence from source
4d4f5b3ed477335e7d90f80badb09f7ef5ea8102 / run 38851975830. Native Find captures
show real-shell Unicode regex matches and the compact bar at 620 width on every
OS; terminal text comes from the actual shell. The existing replay, graphics,
profiles, hotkey, recovery and original Python client checks pass. Unix also
passes native tmux. `v0.8.0-macos-retina` repeats these checks on the exact installed
CI Mac archive at 2×. Local hotkey registration/dispatch is not physical-key proof.
`ci-v0.8.0.json` and `payload-v0.8.0.json` identify exact source/archive provenance.
The first passing candidate is retained separately in ci-first-find-v0.8.0.json;
final source fixes the stale-count race found during review. The first local scripting process-exit timeout is retained separately, with an
idle native-loop stack; clean unchanged-archive rechecks do not erase that failure.
The isolated local
Find benchmark has separate raw measurements/source and excludes UI/PTY costs.

0.6.0 and 0.7.0 evidence remains under their versioned directories, including
global hotkey and original Python API acceptance respectively. Their test counts
and payload provenance are retained in test-matrix.md; these historical results
are not substituted for the current final-package run.

## Historical release 0.4.0

`v0.4.0-osx-arm64`, `v0.4.0-win-x64`, and `v0.4.0-linux-x64` contain final
48-test TRX, native reports/renders and engine benchmarks from run 38700844528,
source 1cf4a149b9f6c17df2b9e7ad4e4a591dc3d61851. In addition to the 0.3.0
baseline, `triggers.png` shows a real shell line highlighted by a profile rule,
`graphics.png` shows shell-generated sixel/OSC1337 PNG/kitty RGBA with verified
red/green/blue pixels, and `preferences-triggers.png` shows the profile editor.
`v0.4.0-macos-retina` records acceptance from the installed final CI macOS archive
on macOS 26.7.2/M5 at 2× scaling. These are native client renders; no simulated
terminal output, OS menu clicks, physical IME or multi-monitor coverage is implied.
`ci-v0.4.0.json` is the successful final run; `ci-first-trigger-v0.4.0.json`
preserves the Windows cold-start policy failure from the first candidate.
Payload checksums/signature/notice audit is in `payload-v0.4.0.json`.

## Historical release 0.3.0

`v0.3.0-osx-arm64`, `v0.3.0-win-x64` and `v0.3.0-linux-x64` contain native
client renders, JSON reports, engine benchmarks and the 36-test TRX suite from
final run 38693452893/build e3a0a02d4c07bfc6e0c1429b4e2514b777f441e8. Captures
cover minimal single session, tabs/splits/find, dark/light themes, profile
General/Text/Colours, application General, and historical text replay. Live
shell output continues while replay seeks an independent historical screen.
`ci-v0.3.0.json` records the passing jobs. macOS menu configuration is tested;
these client captures exclude the system titlebar/menu bar and do not prove
physical menu clicks. `v0.3.0-macos-retina` adds local native acceptance on
macOS 26.7.2/M5 at 2× scaling, including a fixed historical grid during viewer
resize. `retina-before-fix.json` and `ci-before-retina-v0.3.0.json` preserve the
earlier failure and passing 1× candidate rather than hiding the regression.
Physical macOS keys separately verified one-tab creation, pwd and replay opening;
personal shell captures are excluded from public evidence. Physical replay seek
injection was inconclusive and is not credited as a pass.

## Historical release 0.2.0 captures

`release-macos/` contains the final local self-contained package's native rendered
windows on macOS 26.7.2/Apple M5: dark/light initial shell, tabs, nested horizontal/
vertical panes, search and preferences. `native-smoke.json` records successful
input/resize/layout/render/cleanup and scoped startup/memory measurements;
`benchmark.json` records the engine workload; `acceptance.trx` is the full suite.
`release-osx-arm64`, `release-win-x64` and `release-linux-x64` contain the hosted
release-code run's native renders, JSON and TRX on each actual target OS.

Images use Avalonia RenderTargetBitmap on visible native windows from a running
OS backend and genuine local shell processes. They capture the client surface,
excluding system titlebar/Dock decorations. Linux uses X11/Xvfb. They are neither
mockups nor proof of physical keyboard/IME/multi-monitor coverage. Shell commands
produce the visible acceptance text; no simulated output is substituted.
The isolated fixture's deterministic prompt avoids user rc files/personal data.

`iterm2-original.png` is the installed original **iTerm2 3.6.6**, copyright George
Nachman/contributors, captured with native window capture on 2026-10-14. Its
version, startup profile and window geometry differ from the source audit and
final Aster fixture. It is a before reference, not a pixel-equivalent benchmark.
Final Aster captures allow direct inspection of the migrated workflows/themes;
the original AppKit/Metal rendering implementation is not claimed to be reused.

## Historical first-slice captures

Preserved, accurately dated evidence from before preferences/release hardening:

- `aster-initial.png` / `aster-final.png`: real user zsh startup in first developer builds.
- `aster-tabs.png` / `aster-splits.png`: native shortcuts created independent tabs and nested panes.
- `aster-search.png` / `aster-pane-close.png`: search UI and split collapse.
- `aster-native-command.png`: individual per-pid native keys executed `pwd` in a live zsh pane. Bulk AX text insertion was a no-op and receives no evidentiary credit.
- `first-slice.trx`: the original 19-pass/1-Windows-skip run, not current results.
- `iterm2-COPYING.txt`: pinned upstream licence grant.
- `dependency-licenses.json`: restored NuGet metadata; native audit is in ../dependency-source-map.md.
- `ci-38520129077.json`: first complete hosted three-platform pass before final hardening.

Use current release evidence for release claims; historical startup images are
not tests of features that were implemented later. Native renders supplement
engine/UI/process acceptance and do not replace it.

Linux's first final capture exposed an absent CJK font in the minimal runner
image (a missing glyph box despite correct Unicode buffer cells). The native
verification environment now installs fonts-noto-cjk; Linux installation guidance
includes this font requirement. This is system font fallback, not a fabricated
replacement in the screenshot.

Version 0.5.0: `v0.5.0-{rid}` contains final combined CI test/native/recovery/tmux
evidence from the source/run in ci-v0.5.0.json. Windows has no local tmux claim.
`v0.5.0-macos-retina` repeats acceptance on the exact extracted/installed CI Mac
archive at 2× scaling. All published new captures use isolated fixture settings
and /bin/sh tmux panes; no personal shell startup captures are included. Earlier
0.5.0 candidate grid/resize-loop/Windows failures remain separate and accurately
scoped. The standalone `v0.5.0-tmux-local` is an earlier passing local candidate,
not the final package provenance.

Public TRX files normalize Windows CRLF line endings; test values are unchanged.

Version 0.10.0: ci-v0.10.0.json pins corrected source 87184b0 / run 39001628564.
v0.10.0-{rid} contains passing 80-test/native evidence, including tab movement
and retained shell state. v0.10.0-macos-retina contains the exact installed
eight-process/three-scripting batch and normal-launch metadata. The first Linux
candidate failures remain in tab-reorder-investigation-v0.10.0. Full inventory,
version, notice, signature and installed-byte checks are in payload-v0.10.0.json.

## Historical release 0.11.0

- `ci-v0.11.0.json`: final source e283c83 / run 39003323494, all three jobs successful.
- `v0.11.0-{rid}`: final 82-test TRX with OS skips and native baseline/hotkey/recovery/original Python evidence; Unix native tmux, terminal/Settings screenshots.
- `v0.11.0-macos-retina`: exact installed CI archive, full ordered eight-process verifier at 2×, three unchanged original Python passes with clean native exit; metadata-only ordinary launch. `native-caption-first-baseline.json` is a separate first passing baseline PID, clearly labelled.
- `ui-alignment-investigation-v0.11.0`: invalid-style regression failure, corrected local 82-test TRX, first installed pre-connect Python timeout (clean parent exit 1, cause unconfirmed), then instrumented diagnostic pass. These are retained separately from the final unchanged full pass.
- `payload-v0.11.0.json`: archive digests, inventory, required notices, runtime/version, Mac signatures and installed-byte comparison.
- `visual-reference-v0.11.0.json`: attributed reference image hash and sampled colours, not a whole-window similarity score. See ../visual-parity.md for gaps and native captures.
- `rules-v0.11.0.json`: official announcement check for this milestone; no contest submission claim.

## Current release 0.12.0

- `ci-v0.12.0.json`: final source e4e1646 / run 39009937189; all three targets successful, 85 cases with documented OS skips.
- `v0.12.0-{rid}`: final TRX, native baseline/reference-size/settings images, global hotkey, original Python and crash/reopen; Unix exact tmux acceptance.
- `v0.12.0-macos-retina`: exact installed CI archive, eight-process verifier, three original Python clean native exits, plus curated normal 587×462 / Monaco 12 inspection. Private normal-shell screenshots are not published.
- `installed-reference-v0.12.0`: iTerm.app bundle/version, sampled geometry/colours and appearance-only profile values; first failed CI Mac/Windows TRX/logs, corrected local 85-case TRX and scoped first local installed-candidate result. Superseded runs are not final evidence.
- `payload-v0.12.0.json`: archive hashes, inventory/notices, runtime/version, compiler/credential exclusion, Mac signatures and installed-byte comparison.
- `rules-v0.12.0.json`: official challenge recheck; no form submission.


Release 0.13.0 uses runtime source 1a4ff56cbda02076575a11b12966db09383f9b63
and successful native run 39034987649. v0.13.0-{rid} contains only controlled
real-shell fixture captures/reports/TRX and native exit logs. The exact Mac
archive is independently verified at 2× in v0.13.0-macos-retina. New-reference
user shell captures are intentionally not published; pixel-reference-v0.13.0.json
contains lossless signatures/hashes, geometry, sampled colours and selected-text
statistics with explicit limits. No selected crop establishes whole-window parity.
