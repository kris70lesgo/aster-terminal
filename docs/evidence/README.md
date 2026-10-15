# Screenshot and runtime evidence

## Current release 0.4.0

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
