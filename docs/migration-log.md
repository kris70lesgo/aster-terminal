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


## 0.3.0 — minimal iTerm-style UI and bounded replay

- Compared the installed iTerm2 single session screenshot and pinned source:
  terminal-first window, compact typography, no always-visible toolbar/footer.
  Replaced Aster's redundant chrome with native macOS menus, auto-hidden single
  tabs, optional status and compact General/Text/Colours settings. Thin dividers
  retain a seven-pixel pointer target. Existing profile customizations are kept.
- Studied pinned `sources/DVR/DVR.m` and `DVRBuffer.m`: timestamped screen state,
  bounded storage, oldest eviction and a half-capacity oversized-frame guard.
  Rewrote as portable cell keyframes. The AppKit/binary delta codec was not
  imported. No new runtime dependency. Notices record the studied GPL design.
- Added synchronized engine snapshots, per-pane opt-out/clear-on-close recorder,
  and an independent read-only renderer. Seeking never replays into a live PTY.
  Replay tests use real shell-generated screens and delayed output while viewing
  history, plus Unicode/colour/grid preservation and capacity tests.
- Local Release suite: 34 passed, 2 Windows-only skips, 0 failures (36 total).
  Earlier native minimal UI captures passed. The later native/replay launch was
  blocked before any window by CoreVideo -6661; an independent CoreGraphics probe
  reported zero active displays and reproduced the same CoreVideo error. No
  display/session settings were changed. Final native three-OS CI is pending.
- Computer Use rejected iTerm2 app access for safety reasons after the initial
  reference capture. Continued from the existing screenshot and pinned source;
  no alternative route into the denied app was attempted.
- Rechecked the official competition announcement at release preparation:
  public GPL source/builds, screenshots/write-up and deadline requirements are
  unchanged. Form/team declaration and actual submission remain separate.

- First 0.3.0 CI run `38570345791`: macOS and Windows native packages passed;
  Linux failed the older vim fixture. Its output showed capability/CPR queries
  during startup. The fixture had no engine-response routing and typed as soon
  as alternate-screen mode began. Added the same reply routing as TermControl
  and waited for vim's rendered startup screen before input. Also placed the
  widened divider above panes so its overlapping pointer target is reachable.
  Final all-platform rerun is required after these changes.

- Rerun `38571046459`: Linux/Windows native packages passed; macOS exposed
  the tmux fixture exiting immediately after a resize, allowing query replies
  into the outer shell. The fixture now selects a deterministic inner sh prompt,
  resizes both engine and PTY, and confirms the inner pane reports 34×105 (one
  status row) before exit. It still requires return to the shell and less output.
- Replay palette and text are captured under the same lock. Fixed timestamp
  clamping before eviction for a one-frame buffer. Cursor labels in Settings
  now read Block, Vertical bar and Underline; persisted values are unchanged.

- Final code run `38572021565`, source `66aac10d85d4e80845944bfccded96ac0385cd99`:
  macOS/Linux 34 passed + 2 Windows-only skips; Windows 29 passed + 7 Unix-only
  skips; all 36 total / 0 failures. Every native final package passed shell,
  input, resize, replay/continuing output, renders and cleanup. Versioned
  evidence is retained under `docs/evidence/v0.3.0-{rid}`. Mac deep/strict signing
  metadata survives archive extraction and the extracted app passes acceptance.

- Audited all downloaded CI archives against their shipped SHA-256 files and
  required GPL/dependency/runtime/font notices; no user settings, shell history
  or test assemblies ship. Linux executable modes are intact. Mac extraction
  and installed deep/strict signatures pass; bundle version 0.3.0 is installed
  at `/Applications/Aster Terminal.app`. The previous bundle is backed up in
  ignored artifacts. Existing sessions/settings are preserved; local relaunch
  waits for an active display. Audit hashes are in `evidence/payload-v0.3.0.json`.


## 2026-10-15 — Retina replay correction before publication

The display became available and installed-package acceptance exposed a real
Retina-only viewer defect. TermControl's layout reflowed the historical engine
using different rounded font metrics at 2× scaling; with no history, this could
remove historical rows. The 1× hosted run had passed. Release 0.3.0 remains a
draft until this is fixed and all packages are replaced; prior hashes/evidence
are candidate records, not the final published build.

A fixed-grid ReplayTerminalEngine now accepts only explicit historical frames;
control input/reset/resize cannot mutate them. Viewer sizing measures glyphs at
its actual render scale and refreshes on open. Tests cover attempted layout/input
mutation, and native acceptance resizes the viewer while asserting old text/grid
remain intact. The first corrected local Retina package passes native replay,
shell output, render and cleanup acceptance. Expanded final CI is pending.

The initial PTY grid also measures glyphs at the actual window render scale,
avoiding a 1× estimate before a Retina shell starts. Physical per-pid Cmd+T
created exactly one tab, and native p/w/d/Return keys executed pwd in a normal
local zsh session. Personal shell screenshots remain local, outside public evidence.

Run 38692474887 passed macOS/Linux but caught the PowerShell fixture typing
before PSReadLine had produced its prompt on a cold Windows runner. The fixture
now waits up to 30 seconds for that real prompt before its command; command
output/cleanup checks retain their original limits. Modal replay keyboard
Left/Right navigation also passes via routed headless input. Background native
replay key injection was inconclusive; it is not recorded as a physical seek pass.

Native menu review found that Avalonia adds its standard macOS app commands
only when creating the first exporter. Replacing the app menu on activation
could discard Quit/Hide. Aster now reuses that menu and routes About/Settings
through a weak active-window reference. Native smoke explicitly checks Quit
remains after creating a tab. No window/session state is retained by menu closures.

Final release-code run **38693452893**, source
`e3a0a02d4c07bfc6e0c1429b4e2514b777f441e8`, passes on all three targets:
macOS/Linux 34 passed + 2 Windows-only skips, Windows 29 passed + 7 Unix-only
skips, all 36 total and zero failures. Each extracted self-contained package
passes native shell/input/resize/replay/render/settings/session-cleanup checks.
Versioned reports and images now contain this final run; the old 1× candidate
CI record and local Retina failure are preserved separately.

All final archives were downloaded, CRC/SHA-256 checked and audited for required
GPL/runtime/dependency/font notices, absent user settings/history/test payload,
and Linux executable modes. The exact final CI macOS archive was extracted
with ditto, deep/strict signature verified, staged and installed with a backup.
The installed bundle passes native acceptance on this workstation at **2× Retina**
(startup sentinel 507.9 ms, resize 33.0 ms; single observations). Historical grid
survives viewer resizing while the real shell continues. Final payload hashes
and installation evidence are in `evidence/payload-v0.3.0.json`. The earlier
zero-display obstacle is resolved. No personal shell captures are published.

The 0.3.0 release assets are replaced with these final three-platform packages;
source, notices, checksums and evidence accompany the release. The official
challenge announcement was rechecked on 2026-10-15 without a rule change.
Advanced parity and manual-input/platform gaps remain explicit; no contest
form submission is asserted.

## 2026-10-15 — 0.4.0 output rules and verified inline graphics

Continued beyond 0.3.0. Profile rules now highlight matches or bookmark completed
logical lines with a 200-record per-pane history and anchor-based navigation.
The settings editor validates and persists rules transactionally. Existing VT
parsing strips control payloads from matching; rules cannot run host actions.
Fragmented/wrapped Unicode, rewritten text, colour ranges, expired anchors,
settings invalidity/cancellation and real-shell output have acceptance tests.
A performance review replaced full-history capture for ordinary trigger lines
with one-row capture under the existing engine gate. Wrapped rules still need
retained-history mapping; matching work is budgeted and limitations are explicit.

A new scrolled-output regression exposed the dependency resetting scroll offset
on every full-screen scroll. Aster now anchors the viewed line across output;
when it is evicted, the view remains at oldest retained history. Reset/history
clear still returns to the live screen. Tests cover this independently of rules.

Sixel, OSC1337 PNG and direct-data kitty RGBA now pass actual Skia pixel tests
through the Aster adapter, including byte fragmentation and reset. Malformed
payload/oversize dimensions and rejected kitty file transfer recover without
images or host file actions. Native smoke additionally emits all three from a
real shell and checks coloured pixels; native three-platform results pending.
The official challenge announcement was rechecked at this milestone, unchanged.
No dependencies were added. Full reconnect, native tmux -CC, Python API and
system-wide hotkey parity remain unfinished.

First 0.4.0 candidate run 38700487961: macOS/Linux tests and native packages
passed; Windows caught cold-start rule evaluation being skipped by the burst
budget before its first regex ran. The budget now starts after the first rule
evaluation, keeping the first ordinary line eligible despite JIT/capture setup.
Nonbacktracking regexes have a 100 ms timeout (including cold runner costs), with
the following per-Feed work limited by the 25 ms budget. This is an execution
policy repair, not a weakened Unicode assertion. The failure is retained in CI.

Final 0.4.0 source **1cf4a149b9f6c17df2b9e7ad4e4a591dc3d61851**, CI
**38700844528**: macOS/Linux 46 passed + 2 Windows-only skips; Windows 41 passed
+ 7 Unix-only skips; all 48 total, zero failures. All extracted native packages
pass replay, real shell, input/resize, output rule, three inline image colour
pixels, menus/settings/renders and cleanup. The final macOS CI archive is
installed and passes the same acceptance on this Mac at 2× scaling (558.2 ms
startup sentinel, 27.4 ms grid resize; single noncomparative observations).

All downloaded archives passed CRC and SHA-256 comparison, notice/source/font
inventory, absent personal settings/history/test-assembly checks and Linux
executable modes. Mac ditto extraction/staging/installation passed deep/strict
signature verification with the prior bundle backed up. Final hashes are in
`evidence/payload-v0.4.0.json`. Versioned public evidence uses isolated fixtures;
existing user sessions/settings are preserved. Source/evidence and all six
archive/checksum assets accompany 0.4.0. Remaining advanced parity stays explicit.

## 2026-10-15 — live process recovery work

Added a separate C# PTY owner and authenticated current-user pipe transport, private
capability catalog, exclusive reconnect, bounded queues/journal and detached-host
expiry. Original native transports remain unchanged. The window owns/disposes its
frontend; normal close still terminates processes, while crash/lost IPC or explicit
detach keeps the real shell available. Pane startup can reconnect detached sessions.
No environment/output is persisted and no network listener or NuGet dependency is
added. iTerm2 process adoption/socket-handshake source informed the design; no
upstream server code is copied. Journal-overflow text recovery limits are explicit.

Local tests pass for retained shell variables/process metadata, detached output
replay, resize, exclusive ownership, rejected capabilities, actual window detach/
reopen, cleanup, invalid frame lengths and bounded checkpoint compaction. Added
native abrupt-UI-exit/reopen acceptance; native/three-platform verification pending.
A natural process exit initially caused an unnecessary termination reconnect during
Dispose; Close now recognises its received exit before attempting that fallback.

First native hosted candidate failed actual sixel pixels despite passing parser/
transport tests: grid echoes reset cell geometry to 1×1. The pipe now carries
pixel dimensions as well as columns/rows, initial host queries use actual cell
metrics, and frontend replay preserves these metrics. Added a grid/image geometry
regression; the failing report is retained as host-pixels-before-fix.json.

The first native crash/reopen fixture recovered the same shell variable and Unix
PID, but typed its stty check before the control's debounced PTY resize reached
the host. Native verification now waits for the host grid acknowledgement before
checking kernel dimensions, rather than accepting the local renderer grid alone.
The earlier failure remains in recovery-before-resize-ack.json.

Local final recovery suite: 52 passed, 2 Windows-only skips, zero failures / 54
total. Native Mac at 2× Retina passes abrupt UI exit/reopen with actual same
Unix PID, shell-local variable, host/session identity, input, acknowledged resize
and normal-close host/PTY cleanup. Baseline native graphics also passes after
the cell-pixel transport fix. Three-target CI is the next verification gate.

The Windows recovery window fixture exposed cancellation of an accepted PTY
write when its frontend detached. ConPTY treats in-flight input cancellation
as a session cancellation. Host input writes now use host lifetime rather than
attachment lifetime; the accepted frame can finish after UI loss. The pending
frontend queue also drains before an explicit detach request. The failed runs
38734958032 and 38735744022 remain evidence; repaired Windows verification is
pending. macOS/Linux native crash/reopen passed both candidates.

## 2026-10-15 — explicit native tmux control client

Studied pinned iTerm2 TmuxGateway.m / VT100TmuxParser.m and tmux's control-mode
manual. Implemented an independent C# explicit `-C` client, bounded response/
escaped-output parsing, strict classic-layout trees, serialized hexadecimal
input, real viewport hydration and separate native tabs/panes. The server owns
shells; native control-window close detaches. No source or tmux executable is
copied/bundled. Automatic in-shell `-CC`, remote/WSL and full terminal bootstrap
state remain explicit gaps rather than ordinary-tmux parity claims.

Local real isolated-server tests pass independent input/Unicode/colour, native
layout control retention, nested splits, resize, new windows, pane removal and
detach/reattach retaining a shell-local variable. Full suite: 55 passed plus 2
Windows-only skips / 57 total. The first native fixture deadlocked by blocking
Avalonia's initialization context on async process startup; ConfigureAwait(false)
in its non-UI process helper repairs that fixture. Native acceptance pending.

Recovery repair source 5e78e93 / CI 38749039608 passes all three targets: macOS/
Linux 52 pass + 2 skips; Windows 47 pass + 7 skips, all 54 total. Each native
package passes crash/reopen and baseline checks. This confirms the attachment-
lifetime cancellation repair on Windows; no platform support is inferred from
compilation alone.

Tmux raw output may be invalid/incomplete UTF-8 between notifications. The
controller now parses the protocol byte-preservingly with Latin-1 framing and
only decodes command-response text as UTF-8; pane payloads go unchanged to the
incremental engine. A real split-byte λ and Vim edit/alternate/return pass. Vim's
welcome message is absent in narrow panes, so the fixture loads a real initial
buffer marker rather than depending on a welcome banner.

Strict native grid acceptance exposed a geometry mismatch and a focus/refresh
loop. The tmux-owned grid is now applied before ordered output, control layout
updates cell metrics only, native pixel layout accounts for padding/dividers/
cell budgets (control clients have no rendered status row), and unchanged focus does not send repeated select-pane commands.
Failed native reports remain in tmux-grid-before-fix.json and
tmux-resize-loop-before-fix.json. Fixtures use /bin/sh for all panes to exclude
user shell startup files from published evidence. Final local native Retina acceptance passes all four checks, including exact grid/cell fit and same-shell reattachment. Hosted combined verification is pending.

The official challenge announcement was reread at this milestone on 2026-10-15
(curl fallback after web-reader timeout); requirements remain unchanged.

## 2026-10-15 — final 0.5.0 delivery verification

Final source **96a90b4f3fdf45f78d108cc6b4dba5768285e103**, three-target CI
**38752301646**: macOS/Linux 55 pass + 2 Windows-only skips; Windows 48 pass +
9 Unix-only skips; all 57 total, zero failures. Every extracted native package
passes baseline and abrupt crash/reopen with the same shell/process; Unix also
passes real tmux native splits, input/Unicode, kernel dimensions, exact grid fit
and same-shell reattachment. Native menu/keyboard close/find share handlers.

All final archives passed SHA-256 comparison, ZIP CRC/TAR inventory, complete
notice/source/font checks, absence of user settings/history/test assemblies,
and Linux executable permissions. The exact CI Mac archive was extracted with
ditto, signature-verified before/after staged installation, version-checked and
tested again at 2× Retina: baseline, abrupt UI exit/reopen and native tmux all
pass. Prior installed bundle is backed up; existing user processes/settings
are untouched. Public captures use isolated fixture settings and plain shells.

Final payload hashes and signature/installation scope are in payload-v0.5.0.json;
versioned JSON/TRX/captures and CI provenance accompany the source. Performance
reports distinguish UI-only working set from unmeasured process-tree memory.
Automatic in-shell -CC, remote attach, Python API, global hotkeys and broader
manual/platform/graphics parity remain explicit. No form submission is asserted.
