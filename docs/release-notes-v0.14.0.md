# Aster Terminal 0.14.0 — Avalonia Port Challenge candidate

Aster Terminal is a C#/Avalonia rewrite of a documented iTerm2 terminal subset
for macOS, Windows and Linux. This release adds the final contest candidate
features and verified self-contained packages for all three desktop targets.

## Highlights

- Match selected iTerm2 3.7.4 frame measurements and colour samples; use the
  compact single-tab caption, login-shell titles and a native scrollback gutter.
- Fuzzy Open Quickly searches live pane sessions and configured profiles.
- Swap adjacent live panes without recreating shells or changing split geometry.
- Opt-in Smart Case for literal/regex Find, and opt-in smart double-click
  selection for URLs, emails, IPv4 addresses and absolute paths.
- Preserve real terminal processes through tabs, splits, recovery and the
  documented original iTerm2 Python client subset.

## Verified targets

Final source `0d5ca3813703fce55fcdd8801e7c37918729ca74` passes CI run
[39748978754](https://github.com/kris70lesgo/aster-terminal/actions/runs/39748978754).
macOS/Linux each pass 121 of 123 tests with 2 platform skips; Windows passes 112
of 123 with 11 platform skips. All targets pass native packaged window, shell,
resize, rendering, global-hotkey, recovery and scripting acceptance; Unix also
passes native tmux control-mode checks. See the repository verification matrix
and versioned evidence for exact results.

Each archive has an adjacent SHA-256 file and includes GPL and third-party
notices. The macOS bundle is ad-hoc signed but not Developer ID notarized;
Windows is unsigned and may show SmartScreen.

This is a substantial terminal subset, not complete iTerm2 UI/feature parity.
Before/after captures show real applications but are not pixel-identical matched
screenshots. Wayland and broader physical keyboard, DPI, IME and multi-monitor
testing remain gaps. The challenge form has not been submitted by this release.
