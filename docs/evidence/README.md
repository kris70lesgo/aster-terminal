# Screenshot and runtime evidence

## Release captures

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
