# Migration log

## 2026-10-14 — Initialization and first slice

- Empty unborn Git repository; no existing work overwritten. Host macOS 26.7, arm64, installed SDK 9.0.318. Installed verified SDK 10.0.402 in ignored `.research/dotnet`; pinned Avalonia 12.1.4.
- Verified official contest announcement before implementation and wrote `challenge-compliance.md`. Entry form field retrieval failed; marked unverified. No submission made.
- Pinned iTerm2 `069b6e0dd8f3f0d82fc08ea5d294d28f7c5b986e`. Studied VT100 parser/terminal, PTYTask and server job managers, PTYSession, Metal driver, profile model and DVR. AppKit/Metal/server architecture cannot be directly shared across three desktop OSes. Chose a native Avalonia host with terminal and process layers behind package contracts.
- README/GPLv2 discrepancy resolved from COPYING's explicit GPLv2-or-later grant and effective GPLv3 explanation. New/adapted code GPL-3.0-or-later; imported MIT and OFL notices retained. Further upstream submodule reuse and release payload licence checks still pending.
- Evaluated XtermSharp (documented grapheme/reflow gaps), direct Objective-C reuse (platform burden), and embeddable Devolutions control. Selected Devolutions.Terminal.Control 2026.3.4, source `fa23083f1bda1a60fe2b322c6cfab4a616af9edb`. Did not fork the comparable application's UI, Windows Terminal settings UX, cloud integrations or ISE functionality.
- NuGet omitted `dt-pty-host`; package's Unix connection throws if it is absent. Imported pinned MIT C helper unchanged and added host-compiler MSBuild integration. Initial helper-copy path used an early-evaluated IntermediateOutputPath, causing MSB3030; corrected to deterministic obj/pty path and built before PrepareForBuild. Added helper to publish outputs.
- Adapted upstream `VT100Terminal.m` `exitCodeFromArgs`: walk non-key/value args and use first complete integer. Existing dependency only parsed the whole suffix after D; attributes broke status. `ItermTerminalEngine` uses established VtParser/IVtDispatch, normalizes this one protocol, handles display-only CurrentDir/RemoteHost, and preserves base protocol dispatch. No new VT parser or renderer written.
- Added real session tabs/splits, focus borders, copy/paste, search, font-size shortcuts, async session close and serialized layout changes. No mock terminal output. No live session restoration or saved settings claim.
- First test run: 16 passed, one failure, one Windows skip. Failed assumption: dependency maps nonzero normal exit to `Failed` state. Inspected PublishExit; acceptance now asserts ProcessExited reason and exact status 7, preserving dependency behavior.
- Added headless Avalonia acceptance with real PTYs to verify UI event routing, selection/clipboard/paste/search, resize propagation, independent tab/pane processes and close. Avalonia 12 Headless.XUnit requires xUnit v3; resolved v2/v3 conflict and updated source-aware fact attributes. Result: 18 passed, one explicit Windows skip.
- Native window launched and displayed actual user zsh startup output. Driver shortcuts opened two tabs, nested horizontal/vertical splits, find bar and closed a pane. Native text injection via driver reported success without terminal changes, so it is not accepted as keyboard evidence. Headless input pipeline + real PTY proves application routing; physical macOS keyboard/IME remains a manual check.
- Follow-up native capture after pane focus: individual key events typed `pwd` and Return, and zsh printed the user's home directory. `aster-native-command.png` establishes native key-event input → real shell → rendered output. Bulk AX insertion remains unreliable; physical-input/IME/native clipboard checks are still pending. Added process cleanup acceptance for the actual shell PID and a spawned background sleep, beyond just helper reaping.
- Captured installed iTerm2 3.6.6 and Aster native windows. These are preliminary startup references at different sizes; not final equivalent-state contest comparisons. No synthetic screenshots.
- Release build passed with zero warnings/errors; local osx-arm64 framework-dependent publish passed. Public downloads, self-contained packaging/signing and Windows/Linux runtime evidence pending.
- Final Release test run: 19 passed, one Windows skip. Additional cleanup test initially encountered bash history expansion of `$!` in interactive sh; disabled history expansion only in that fixture before capturing child PID. Verified both shell and child gone after close. Final osx-arm64 developer publish launched as Aster Terminal; earlier test windows closed through normal pane-close workflow.

No measured development hours, AI credit costs or comparative performance numbers are recorded. Commands and evidence are in `test-matrix.md` and `evidence/`.

## 2026-10-14 — Competition release completion

- Added transactional profiles/preferences, font/colour/cursor/history/env settings,
  dark/light/system appearance, shortcut validation, native menus, pane zoom/focus,
  tab titles/rename, Open Quickly, multiwindow, scrollback export and atomic JSON
  settings with backup/corrupt-original preservation. Reopened layouts start fresh
  child processes; no reconnecting-session claim.
- Bundled original bash/zsh integration from pinned upstream gitlink
  bcd52317a70f1a489b86fcfd1ee97df155741092. Preserved user startup and ZDOTDIR;
  modified only the zsh terminal-name gate with attribution. Real isolated-shell
  tests verify cwd, marks and positional semantic exit status.
- Installed tmux locally and added ordinary tmux/less compatibility fixtures with
  unique test server names. These do not touch the user's tmux server or prove -CC.
- Created public GPL source at https://github.com/kris70lesgo/aster-terminal and
  hosted acceptance/package jobs. Initial Windows/macOS jobs passed; Linux caught
  a global zsh compinit prompt and a shell background process retaining its PTY.
  Isolated only the fixture's global startup and adapted the native helper to HUP
  members of its own Linux PTY session. The next three-platform run passed.
- Native light-theme inspection exposed unapplied terminal colours/font size;
  fixed actual engine Scheme and font reset, with live-setting assertions.
  New light panes also received a contrasting status colour. Expanded native
  acceptance renders initial window/tabs/splits/search/preferences on each OS.
- Expanded tests exposed renderer disposal racing a queued UI render after the
  library's ConfigureAwait(false) disposal. The pane now detaches its control and
  drains queued rendering before closing it. Full suite and targeted nested
  split/close/reopen regression pass after the repair.
- Added real Windows PowerShell acceptance, OSC8 safety/input-mode regressions,
  self-contained native-final-executable smoke tests and benchmark JSON. Measured
  costs are recorded with environment/scope; no comparative performance claim.
- Audited pinned native Skia build targets and DEPS against broad shipped notices,
  recorded licence alternatives/source URLs and retained complete .NET/framework/
  font/native notices. Packages contain GPL text, notice, audit and source map.
- Packaging uses ad-hoc macOS signing and unsigned Windows archives; documented
  installation instead of asserting notarization/signing. macOS bundle minimum
  15 matches current .NET support, and hosted job moved to macos-15.

Milestone A is a release of the tested subset; all advanced unimplemented or
unverified behaviors remain in the parity matrix. Form submission and entrant
identity have not been fabricated. No human-time or AI-spending ledger exists.

Release verification follow-ups:
- A local RID publish had rewritten the main lockfile, causing NU1004 on all
  hosted restores. Declared the three release RuntimeIdentifiers in both
  production projects and forced locked restore during packaging. The corrected
  Linux/macOS jobs passed, establishing a reproducible fix rather than bypassing
  locked mode.
- PowerShell 5.1 Console.WriteLine encoded the non-ASCII test character as `?`
  before the terminal received it. Changed the fixture to PowerShell host output;
  a terminal cannot recover characters lost inside a program's legacy encoding.
- Native smoke now waits for a real deterministic shell prompt before typing,
  rather than treating an allocated PTY as shell readiness. Fixture prompt is
  set in the launch command because an outer login shell may reset inherited PS1.
  Failed acceptance captures buffer/render diagnostics and never publishes a
  passing package. Desktop backend checks require an awake macOS display.

- Final Linux render inspection caught missing CJK glyphs in the minimal runner,
  despite correct Unicode buffer tests. Added fonts-noto-cjk to the native test
  environment and documented it as a Linux font-fallback requirement. System
  fonts are host dependencies, not bundled and relicensed by the app.

- Downloaded Mac archive launched, but strict code-sign verification detected
  missing extended-attribute signatures on managed .NET assemblies. The source
  bundle verified; Python ZIP had dropped those attributes. Mac packaging now
  uses ditto/AppleDouble metadata, extracts the archive, verifies deep/strict
  signatures and runs native acceptance from the extracted executable. This
  repairs a distribution defect without claiming Developer ID notarization.

- Installed the verified Mac app under /Applications and launched its real default
  zsh profile through LaunchServices in the background. Inspection also aligned
  the window's grid-size status with post-layout terminal dimensions.

- A later Linux SIGINT test caught a fixture timing race: the next command was
  sent while interrupt handling could still flush terminal input. Readiness now
  comes from inside the foreground child, and subsequent input waits for the
  shell's returned prompt. This verifies actual interruption without arbitrary
  sleeps or weakened output assertions.

- Corrected a stale pane caption after startup: its measured initial row count
  could differ from the arranged engine grid by one row. Pane status now reads
  the actual engine dimensions after startup, matching the PTY resize path.
