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

## 2026-10-15 — system-wide summon window work

Studied pinned iTerm2 iTermCarbonHotKeyController.m and OS SDK headers / primary
Win32/XCB documentation. Added opt-in native registrations with explicit errors
and cleanup, one lazy dedicated real-terminal window, settings persistence and
a manual show command. No package is added; system libraries remain OS-provided.
Wayland, custom global gestures and upstream profile/dropdown styling are outside
this first subset. Terminal output cannot enable or invoke the global shortcut.

The real-shell hide/show test caught Opened firing again after a hidden window
returned: startup was replacing its session. Initialization is now guarded once
per MainWindow. Tests verify the same pane/session and shell variable, disabled-
binding accessibility, normal close/recreation, preserved normal layouts and
visible registration failure. Native backend/physical-focus verification pending.

Local full hotkey-work suite: 57 passed, 2 Windows-only skips, zero failures /
59 total. Native macOS 26.7.2 at 2× scaling registers/unregisters the actual
Carbon backend and sends a typed native hotkey event through its dispatcher to
the checked callback. No local global physical key was injected and foreground
was preserved. Actual cross-application focus remains pending CI/manual evidence.

First hotkey CI 38756104154: Windows and Linux X11 pass the complete native
cross-process global chord, real-shell hide/show retention and cleanup. macOS
also registers, dispatches the actual injected chord from another foreground
process and summons/focuses a real shell. Its subsequent hide passes, but the
fixture then waited for the previous peer to regain foreground without asking
macOS to activate it. The CI-only peer helper now explicitly reactivates its
own peer before the next injected chord. This is fixture focus preparation,
not a removed assertion or a local foreground action. The failing report and
run remain in hotkey-macos-peer-before-fix.json / ci-first-hotkey-v0.6.0.json.

Second hotkey CI 38756865651: macOS and Windows pass all native/global checks.
Linux also summons/focuses its terminal and executes the retained-variable
command, but dash can print the next prompt immediately before its output when
two queued commands are entered together. The fixture's exact-line assertion
rejected `ASTER_READY> __HOTKEY_READY__retained` even though the expanded value
was correct. It now checks the actual expanded sentinel without requiring a
line-start position. That complete sentinel cannot come from the echoed input,
which contains the unexpanded variable / printf placeholder. Session identity,
retained variable, physical global focus and cleanup assertions remain intact.
The failing JSON and actual native image are retained separately.

Run 38758404103 passes all global-hotkey checks (including Linux), then Linux's
subsequent crash/reopen fixture encounters the same queued-dash prompt prefix
before its before-crash sentinel. The actual value is present; its strict
line-start wait times out. Recovery fixture printf output now starts with an
explicit newline, preserving its existing strict sentinel/PID assertions.
The failed report remains in recovery-linux-prompt-before-fix.json. This change
only delimits isolated fixture output; it does not change terminal behavior.

## 2026-10-15 — detach output/disposal race found during 0.6.0 verification

The Linux candidate run 38759175327 failed the real-shell window recovery test.
An isolated immediate-input/detach/reattach regression also failed on this Mac.
Opt-in temporary lifecycle tracing identified late PTY output reaching an already
completed subscriber queue and calling Cancel on its disposed attachment source.
That ObjectDisposedException escaped the output callback into the native adapter,
which reported a PTY fault and shut down the host. The retained failing trace is
`evidence/detach-disposal-before-fix.txt`; it contains only isolated fixture output.

Attachment cleanup now removes both publish references under the same gate as
PTY output, before completing the channel or disposing cancellation. Both reader
and writer tasks are awaited even when one faults. Explicit detach additionally
waits for a host acknowledgement after prior native input writes complete; the
optional greeting capability preserves connection to older hosts. The regression
performs eight immediate queued-input/detach/reattach cycles against one real
shell and checks its expanded variable after each reconnect. Temporary trace-file
support was removed after diagnosis. Local full suite passed 58 + 2 skips / 60;
15 focused runs (120 immediate detach/reconnect cycles) also passed. Final
native/three-OS checks are pending below.

The local human hotkey attempt was not detected; its 180-second fixture expired.
It is unverified, not a physical-focus pass. A later read-only macOS check reported
secure event input disabled; that does not establish its state at the attempted
keypress. Earlier CI global-key checks are recorded separately.

## 2026-10-16 — 0.6.0 final package verification

Source c651f5bad692a55f1fd36c27442e4aa5c2da1400 / run 38763544008 passes
macOS/Linux 58 + 2 skips and Windows 51 + 9 skips, all 60 total and zero
failures. Every self-contained package passes native baseline, abrupt UI
crash/reopen and actual global chord with isolated peer foreground; Unix also
passes tmux. Exact CI Mac archive was extracted with ditto, signature checked,
installed and retested at 2× Retina. Native baseline/recovery/tmux/registration
pass. Its human physical attempt remains unverified. Archive checksums, CRC/tar
inventory, Linux executable modes, notices, absence of user/test payload and
installed/extracted Mac signatures pass; payload-v0.6.0.json records hashes.
Official challenge announcement reread at this milestone; requirements unchanged.
Entrant declarations and form submission remain separate and unperformed.

## 2026-10-16 — original Python API core implementation

Studied pinned proto/api.proto, iTermAPIHelper/iTermAPIServer and the original
Python Connection/App/Window/Session/RPC implementation. Mature Google.Protobuf
3.36.2, build-only Grpc.Tools 2.84.0 and ASP.NET Core 10.0.13 replace the macOS
server without a handwritten WebSocket/protobuf codec. Copied only the attributed
GPL schema, with a C# namespace option; source/license provenance and full notices
are recorded. All three ASP.NET runtime nuspecs pin the same audited source SHA.

An off-by-default loopback server authenticates private per-user cookies,
validates Origin/Host/subprotocol, bounds frames/rate/peers/queues and holds an
exclusive credential-directory lease. Existing UI methods perform actual tab,
split and close actions. Live viewport text/styles use an engine-lock snapshot
without changing user scroll. The Python launcher replaces only upstream transport
and authentication, so it cannot discover/control a running native iTerm2 or
request its AppleScript credentials. Other APIs return explicit errors.

Local native original iterm2 2.30 / protobuf 7.36.2 / websockets 17.2 passed real
session listing, subscriptions, tab/split, shell-local variable, wide/combining
Unicode screen reads, updated layout and closing only owned sessions. Python
3.14.7 on this Mac was used. Candidate report's scale was read after window close
and incorrectly reset to 1; fixture now saves the scale at capture time. The first
report is retained as local candidate evidence, not a Retina claim. Combined
post-0.6.0 local full suite: 63 passed + 2 Windows-only skips / 65 total. Final
three-platform native/package and installed Retina verification remain pending.

The first combined source 2ebb710 / run 38791979700 passed all three platforms,
including native original Python client operations and existing recovery/hotkey/
tmux acceptance. A further local original-client fixture creates a separate real
window, closes its only tab, and confirms the original window/shell remains.
Six focused scripting tests now include the preferences service's default-off,
enable/idempotent reconfigure/disable/re-enable and exit-with-connected-client
lifecycle; credentials rotate/remove correctly. Final run will repeat this added
window and lifecycle coverage. macOS input-source shortcuts 60/61 were read-only
checked and disabled; they do not explain the earlier undetected human hotkey.
No OS shortcuts or security settings were changed.

## 2026-10-16 — 0.7.0 final native/package acceptance

Final source ef6d64269f4825ae1b9947b8269f052bb1cb114c / run 38792785139
passes macOS/Linux 64 + 2 skips and Windows 57 + 9 skips, all 66 total, zero
failures. All final native packages pass original Python client separate-window,
tab/split/input/Unicode/layout/close acceptance plus baseline/recovery/hotkey;
Unix additionally passes tmux. The exact Mac CI archive was ditto-extracted,
signature checked, installed and retested at 2× Retina. Its first Python test
helper mistakenly resolved the venv executable to base Python and timed out;
that report is retained. Keeping the actual venv path fixes the helper, and the
unchanged exact archive passes with Python 3.12.13 and all pinned dependencies.

Archive SHA-256/CRC/tar inventory, Linux executable modes, installed/extracted Mac
signatures, complete notices, ASP.NET/Protobuf runtime versions, and absence of
compiler/test/user settings/API credential payloads pass. Payload hashes are in
payload-v0.7.0.json. New runtime/build licences and source metadata are retained.
Official rules rechecked via curl after a web-reader timeout; unchanged requirements.
Entrant declarations/form submission remain separate and unperformed.

## 2026-10-16 — bounded native Find implementation

Studied pinned iTermFindDriver.m modes and the pinned MIT Devolutions
TextBufferSearch/TerminalSearchSession. The embeddable control provides literal
case/word search, with full-history snapshots; it has no regex option. Adapted
its NFC/grapheme/Unicode-word mapping with original copyright/MIT notices.
Alternatives were a control fork or another regex engine; using a bounded
Sessions search and System.Text.RegularExpressions avoids either dependency.

Background search now returns logical anchors with case/word/regex options,
partial-result warnings and query/cell/text/time/match bounds. UI only handles
cancellation, navigation and visible overlays. Captures preserve scroll; anchors
survive reflow, expire with eviction and reject reset/buffer changes. Closing or
switching a pane cancels work and removes highlights. Regex operates on logical
lines, not arbitrary hard-newline text; explicit dialect/selection/limit boundaries
are recorded in architecture.md. The compact toolbar retains literal-insensitive
defaults. Eight focused tests pass locally, including actual PTY/UI flows. Native
final-package acceptance and the complete cross-platform suite are pending.

Release review found a revision race: a background result could credit output
that arrived after its snapshot as already searched. Find now atomically credits
only its own navigation revision; later output keeps the stale-count indicator
and forces Enter to refresh. A deterministic regression checks both repeated
cached navigation and output after capture, including a previously empty result.
The first candidate run remains separate; final acceptance repeats the fix.

## 2026-10-16 — Find release verification and remaining local failures

Final source 4d4f5b3ed477335e7d90f80badb09f7ef5ea8102 / run 38851975830
passes all three native targets. macOS/Linux: 73 passed + 2 Windows-only skips;
Windows: 66 passed + 9 Unix-only skips; 75 total, zero failures. Final packages
pass real-shell Find options/Unicode/navigation/errors/compact layout plus prior
replay/rules/graphics/profiles/hotkey/recovery/Python checks; Unix adds tmux.
Native Find captures were visually inspected on all three OSes and at local 2×.

Exact installed CI Mac baseline/recovery/tmux/hotkey checks exited cleanly.
Two ordered local batches completed scripting API/PTY assertions but left the
process idle in the native Avalonia/NSApplication loop past 70 seconds. Three
fresh standalone scripting checks against the unchanged archive passed and
exited. This is composite independent local evidence, not a clean ordered batch;
process IDs/scope are retained in v0.8.0-macos-retina/process-exit.json. Only the
owned stuck fixture processes were terminated after sampling; normal user apps
were preserved. Cause remains unresolved and no shutdown fix is claimed.

Temporary diagnostic-only builds added fixture lifecycle logging. One original
client timed out before completing its script and then exited normally; another
native tmux check timed out waiting for the expected stty dimensions. Their
failure reports are retained, separately labelled as diagnostic builds. Logging
was reverted and the exact passing CI archive was reinstalled before final audit.
An external verification-helper trial reused an existing report directory and
could read a prior result before the new process finished; those reused trials
are excluded. The three credited original-archive scripting repeats each used a
fresh directory. Future verification must reject existing report directories.

Final archive SHA-256/CRC/tar inventory, notices and absent private/test/compiler
payloads are audited. Mac extracted/installed signatures and byte-identical main
assembly are verified. No dependency was added; the MIT search mapping retains
its attribution/grant. Official rules were rechecked at this milestone. Local
6000-line search capture/matching costs and allocations are measured separately;
no comparative performance or unconditional platform reliability claim is made.

## 2026-10-16 — scripting exit investigation, 0.8.1 in verification

Fresh 0.8.0 baseline/recovery passed. The ordered tmux check reproduced an old
stty result (28x55) after layout metadata reported 22x42; later unchanged runs
passed. The fixture now queries the owned PTY and waits for actual kernel size
before sending the strict shell stty acceptance command, retaining both values.

PID/phase logging reproduced the scripting stall after successful Python checks,
server shutdown, window cleanup, Exit event and desktop.Shutdown return, with no
return from the native main loop. A first periodic-event-only trial still failed
one of three runs and was discarded. The replacement native AppKit bridge
defers a graceful stop plus an application-defined event to a fresh main-queue
turn; this is Aster GPL code using system APIs, not a framework fork. Six native
scripting checks and a full ordered Retina batch pass with actual PID exits and
main-loop markers. No forced exit or physical key injection is used. A debugger
inspection of the failed process was not counted as a successful run.

A separate deterministic regression held a desktop request incomplete during
server stop. The old transport waited for that action; bounding the wait by the
server token fixes it, releases the credential lease and allows a replacement
listener while the original action is still held. The focused five transport
tests pass. The 76-test suite passes locally (74 + 2 Windows-only skips). One
earlier run paused a trigger on its documented regex wall-time budget during
concurrent diagnostic/build load; the five trigger tests and complete quiet
rerun passed without changing regex safety limits. That failed run is retained.

Final source/package verification on three OSes is pending. The new installed
Mac verifier rejects reused report folders and requires matching PID plus process
exit and clean main-loop return, correcting the earlier helper's stale-report
weakness. Raw failure/phase evidence is retained rather than overwritten.

Final 0.8.1 source **d23eba2abfad621a8f3874cca4a9d98893538f98**, native
run **38901660170**, passes all three platforms: macOS/Linux 74 passed + 2
Windows-only skips; Windows 67 passed + 9 Unix-only skips; 76 total, zero
failures. All final native packages pass baseline, recovery, global hotkey and
original Python checks; Unix also passes tmux. Clean native-loop return markers
are present. The exact installed Mac CI archive passes one complete ordered
Retina batch and three scripting runs, with matching PIDs and actual exits.
Together with six candidate successes, this addresses the reproduced local
shutdown path without claiming universal lifecycle or full upstream parity.

Payload audit passes SHA-256/CRC/tar inventory, Linux executable permissions,
all notices and absence of user/test/compiler/credential payloads. Extracted and
installed Mac native/managed bytes match; deep/strict signatures pass. The new
ARM64 bridge targets macOS 15.0 and links only system libraries. Final hashes
and verification scope are in payload-v0.8.1.json. Rule requirements remain
unchanged at the release milestone. No entrant/form submission is claimed.

Final release-time normal launch was blocked before application initialization:
CoreGraphics reported zero active displays and an independent CoreVideo probe
returned -6661, matching Avalonia's RenderTimer failure. This does not supersede
the earlier passing installed native sequence while the display was active;
the later launch is recorded separately and is not a normal-window pass.
Temporary diagnostic instrumentation was removed; no display/session settings
were changed. Evidence: `evidence/lifecycle-investigation-v0.8.1/final-display-unavailable`.

## 0.9.0 work — shared Find in native tmux panes

Studied the pinned iTermFindDriver query/mode/view separation and reused Aster's
bounded matcher instead of implementing a second search path. Extracted the
existing compact controls into FindBar; both normal and tmux windows use it.
Pane/tab changes, pane removal, Escape and detach cancel/unsubscribe old work.
Tmux metadata refresh no longer steals Find focus, and content-size changes
resize tmux grids when the bar appears/disappears. Creating a tmux window now
selects its new tab. No new package or terminal protocol behavior.

The first regression failed because the tmux window had no compact Find bar.
After implementation, four focused desktop tests pass and the full local
Release suite passes 76 + 2 Windows-only skips (78 total, zero failures).
Native tmux acceptance now exercises the same options/Unicode/navigation/errors,
620-width screenshot, pane isolation, grid fit with the bar open and pending
Find cancellation before detach. Target-OS CI and local installed acceptance
remain pending at this source checkpoint.

Final source 78376eec31ba30d01e3c31c91fd0ac56fafab958 / run 38906845116
passes all three native targets. macOS/Linux: 76 + 2 Windows-only skips; Windows:
67 + 11 Unix-only skips; 78 total and zero failures. Native Unix tmux reports
verify new Find controls, Unicode, compact layout, exact grids and cancellation
before detach; inspected Mac/Linux captures have usable controls/highlights.
The exact CI Mac package is installed, but local acceptance exited before its
report while a fresh CoreGraphics probe returned zero active displays and
independent CoreVideo creation returned -6661. That failed run is retained and
is not credited as local GUI/Retina/graceful-exit support. Target-native CI is
separate passing evidence. Final payload/signature/notice audit precedes release.

The final 0.9.0 payload audit passed for all three exact CI archives: checksums,
ZIP CRC/tar inventory, Unix executable modes, required GPL/runtime/dependency/
font notices, no compiler/test/user-settings/API-credential payload, and Mac
signatures/native system links/macOS 15 target. Installed managed/native bytes
match the extracted Mac archive. The audit explicitly records the failed local
GUI scope rather than promoting earlier-version Retina evidence.

2026-10-16 installed 0.9.0 follow-up after the user awakened the display:
CoreGraphics now reports one active display and CoreVideo creation succeeds.
Unchanged exact installed CI bytes/signatures verified, then a fresh ordered
baseline/recovery/tmux/hotkey/three-scripting batch passed at 2× Retina on
macOS 26.7.2. All eight PIDs exited; each scripting journal records native-loop
return with exit code 0. Inspected normal/tmux compact Find captures have usable
controls and correct Unicode highlights. Evidence is in v0.9.0-macos-retina; the
earlier failed zero-display attempt remains preserved separately. No code or
release archive changed, and no local physical global chord pass is implied.

## 2026-10-17 — Live tab ordering and release version consistency

Studied pinned iTerm2 069b6e0, PseudoTerminal.m moveTabLeft/moveTabRight and
moveCurrentTabByOffset. Ungrouped keyboard movement wraps while keeping the
selected session. Cocoa/tab-group machinery is not portable; Aster uses an
ObservableCollection move of existing TabItems and synchronizes the workspace
list under the existing layout mutation gate. Pointer gestures stay in a
separate window partial, with threshold, insertion marker and cancellation.
This preserves real connections, split/zoom, Find focus and scripting IDs.
Scripted insertion uses the same operation; persistence captures visible order.

Two new real-shell desktop facts pass: both drag directions, ordinary clicks,
Escape/outside cancellation and close-button exclusion; keyboard left/right
wrap, Find focus/results, zoom/split, shell-local variable and process identity,
scripting/saved order and complete cleanup. Full local Release suite: 78 passed,
2 Windows-only skips, 80 total, zero failures. Native smoke now checks keyboard
movement, identities and the shell variable; target-OS CI remains pending.

The About dialog was still hardcoded to 0.8.1 in the released 0.9.0 app. It now
reads the assembly version. Packaging reads App.csproj's version rather than a
second literal. Version 0.10.0 introduces no dependency or protocol change.
Full upstream parity, grouped/pinned/cross-window tabs, pane dragging and tmux
server ordering remain separate targets. No native runtime claim at this checkpoint.

First native CI candidate a226a15 / run 39001432021 exposed two Linux test
synchronization races; failed TRX retained in tab-reorder-investigation-v0.10.0.
Ordinary tmux read its previous kernel PTY size because the nested resize had
not completed. It now polls only the owned pane device before the unchanged
exact stty assertion and always disposes its unique server. The Find test
observed a newly discovered pane's initial empty state before async selection
and query binding; it now waits for that pane's actual completed query. No
production tmux/search behavior was changed. Scripted indexed tab insertion
also gets an explicit original-wire order/selection regression check.

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

## 2026-10-17 — iTerm2 visual alignment, 0.11.0 candidate

The user prioritised UI correspondence. Reviewed the existing attributed 3.6.6
capture and pinned 069b6e0 minimal/Tahoe tab styles and PreferencePanel.xib, plus
current official appearance documentation. Main differences were the extra
caption/tab layers, non-stretched headers, close-button placement and Fluent
settings underlines. New TerminalTabs/WindowButtons/WindowCaption keep native
Avalonia controls and existing pane/session ownership while sharing a compact
caption, real window actions and tab insertion button. Settings gets functional
icon categories and profile segments. Dark token samples match the saved
reference. No upstream asset or new dependency was introduced.

The source capture/version limits, remaining UI gaps and verification scopes
are explicit in visual-parity.md. New desktop tests use owned real shells and
verify caption actions, compact/overflow tabs, minimum Settings navigation and
cleanup; existing tab/Find/tmux/persistence tests are retained. Native acceptance
now captures Appearance and compact Settings. Exact Windows/Linux/macOS and
installed Retina verification remain pending at this source checkpoint.

The final local source passes 80 tests + 2 Windows-only skips (82 total).
A late inactive-caption selector initially built but failed template loading in
the new desktop regression; moving its descendant rule out of ControlTheme
resolved it. Failure evidence is retained in ui-alignment-investigation-v0.11.0.
Earlier CI candidates were cancelled before release; final native checks must
use the corrected source. Settings icons are original outlined vectors, and
inactive title/tab dimming follows the reference's background-window treatment.

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

## 2026-10-17 — installed iTerm2 3.7.4 comparison, 0.12.0 source checkpoint

The user supplied current screenshots and explicitly authorised inspecting the
installed app. `/Applications/iTerm.app` identifies its bundle as iTerm2 3.7.4.
Read-only per-window captures show a 587×462 logical window at 2×, Monaco 12,
#21212F caption / #15191E terminal, no single-tab insertion button and an actual
scrollback gutter. Private shell screenshots stay in ignored artifacts; curated
measurements are in installed-reference-v0.12.0/reference.json.

Aster's saved profiles still selected Cascadia Mono 14, and the pinned renderer
also multiplies point sizes by 96/72. The app now converts nominal Cocoa-style
logical points at the profile boundary (12 becomes renderer 9), including font
zoom increments. Existing stored nominal values and chosen fonts are retained;
Profiles/Text has a transactional reference preset to choose Monaco 12 on Mac
or the bundled Cascadia Mono fallback elsewhere. This changes the rendered size
of existing profiles by 3/4, correcting the unit mismatch rather than rewriting
user settings. Shell startup scripts/content are untouched.

The single-tab plus is hidden; the numbered-window hint has actual app-local
Option+Command+digit (Mac) / Alt+digit navigation. Shared caption spacing/colours
follow the current capture. A native Avalonia scrollbar binds to real retained
history through TermControl viewport APIs, disables in alternate screen and
unsubscribes on disposal. Each tmux pane reserves its extra 12 logical points
when calculating the kernel grid. Main windows can shrink below 620 width to
match the reference geometry. No dependency/VT host action was added.

Release build passes with zero warnings/errors. Local full suite: 83 passed,
2 Windows-only skips, 85 total, zero failures. New real-shell tests cover thumb
drag/history/bottom/alternate screen, numbered-window activation/renumbering and
preset save/cancel/nominal font metrics. Native package verification on all three
OSes and exact installed acceptance are pending at this source checkpoint.

The first local installed candidate (f833e68) passed six ordered native fixtures,
including tmux's exact kernel grids and original Python clean exit. A final
1-point thumb/track margin refinement matches the measured 8.5-point thumb
inside its 12-point gutter; corrected local source again passes 83 + 2 skips.
Final native CI must use the new source; earlier candidate evidence is labelled
separately in installed-reference-v0.12.0/first-installed-verification.json.

First CI source f833e68 / run 39009533442: Linux passes, while Mac/Windows
headless regressions find caption number 2 where a fresh application expects 1.
The process-wide weak list could retain another application lifetime's visible
windows and re-add a window on repeated Show. The registry is now scoped with a
ConditionalWeakTable per Application, filters hidden windows and deduplicates
Show registration. A regression additionally hides/shows twice and checks the
live numbering and navigation. Failed target TRX/logs are retained in
installed-reference-v0.12.0. The superseded margin-only run 39009681029 was
cancelled; no release was published from either. Corrected source again passes
the full local 85-case suite (83 pass + 2 skips); final three-OS CI is pending.

The default 16-colour table also differed: it still used the dependency's
Campbell scheme. Appearance-only reads from the installed iTerm profile now
provide the reference table plus foreground/cursor/selection defaults. Screenshot
colour samples and stored profile colours are recorded separately; no shell
command/environment, credential, startup file or upstream asset was imported.
Explicit foreground/background overrides and terminal truecolour remain intact.

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

### 2026-10-17 — lossless pixel-reference rendering candidate (0.13)

- Compared installed iTerm 3.7.4 and owned Aster windows using true PNG captures.
  Found automation-inherited NO_COLOR and JPEG-encoded snapshots had obscured
  earlier comparisons. Corrected the installed verifier's clean one-shot launch;
  explicit application/profile NO_COLOR behaviour remains intact.
- Added the pinned MIT control/render source as a separate presentation assembly,
  with original hashes and retained notices. Engine/parser/connection stay pinned.
- Aligned 7×17 Monaco cells, asymmetric margins, bold ANSI foregrounds, Mac sRGB
  Metal surface, grayscale/native rasterization, synthetic bold/italic and native
  emoji presentation. Replay/tmux share the same dimensions; no simulated output.
- Local tests: 99 pass + 2 Windows-only skips (101 total). A preceding geometry
  test setup race is fixed with AvaloniaFact. Native candidate baseline, recovery,
  tmux exact-grid and hotkey fixtures pass; initial Python-client fixture timed
  out before its first progress entry and exited cleanly with failure. Retain
  artifacts/pixel-final-installed; do not treat it as successful final acceptance.
- Full final native matrix and installed verification remain required. Lossless
  cyan/red/background samples match; whole-window pixel identity is not claimed.
- Official contest page rechecked at this rendering milestone: open source,
  runnable per-target builds, before/after images and migration write-up still
  required; 23 October 23:59 UTC deadline unchanged. No form submitted.

- First native CI 39013423804 passes Windows and macOS (101 cases each).
  Linux's tmux fit assertion still used the old 10-pixel vertical margin although
  the actual pane and PTY grids matched; changed all fit checks/replay/layout to
  shared profile padding constants. Preserve the failed run as evidence.
- Installed checks exposed a synchronous hotkey-fixture shutdown during Opened,
  before Avalonia's native loop starts. Dispatch the fixture after the loop starts;
  all non-crash fixtures now require matching PID/clean main-loop-return markers.
  Startup failures are logged in process.log, including the observed no-display
  render-timer error (-6661). The verifier temporarily wakes/holds the display
  and releases that assertion after its owned runs. Original NO_COLOR opt-outs
  and foreground application remain untouched.

- Strict final verification caught bootstrap logging creating the scripting
  report directory before its freshness guard. Keep bootstrap `.process.log`
  beside the directory, preserving the guard. The failed 39013686122 run is
  retained; it is not final acceptance. Adjusted the losslessly measured gutter
  to 14 logical pixels (11-point track, 3-point right inset), bottom inset and
  8-point thumb, with the corrected sRGB track colour. Pane sizing reserves the
  same gutter constant on every target.


### 2026-10-17 — final 0.13.0 rendering acceptance

Final runtime source 1a4ff56cbda02076575a11b12966db09383f9b63, native CI
39034987649: 101 cases each, Mac/Linux 99 pass + 2 skips, Windows 90 pass +
11 skips; all native package fixtures pass. Corrected remaining old 12-point
assertions to the shared gutter constant; superseded 39034902148 was cancelled.
The exact CI Mac archive passes all eight ordered 2× installed fixtures, including
three original Python-client runs, with matching clean-loop-return markers.
Signed extracted/installed main and presentation assemblies and native bridge
match; final payload notices, archive checksums and forbidden-data checks pass.
A fresh normal exact-CI window (PID 65553 / 7385) was captured as a true PNG;
header, terminal, cyan and red samples match. Shared date/quote baselines align,
with residual edge differences documented without a whole-window score. Only
curated statistics are published; private shell images remain ignored. Original
two-tab startup layout was restored after closing only owned temporary windows,
while current appearance/other settings and older user sessions are preserved.
