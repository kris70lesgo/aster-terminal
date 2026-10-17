# Aster Terminal architecture — competition release

Working name: Aster Terminal. Independent iTerm2 behavior migration; no endorsement implied.

## Decision, 2026-10-14

Use .NET SDK **10.0.402**, Avalonia **12.1.4**, and **Devolutions.Terminal.Control 2026.3.4**. SDK verified at [Microsoft's .NET 10 download page](https://dotnet.microsoft.com/en-us/download/dotnet/10.0); Avalonia verified at [NuGet](https://www.nuget.org/packages/Avalonia/12.1.4). SDK installed locally in ignored `.research/dotnet`, leaving existing SDK intact.

Devolutions package source commit from its nuspec: `fa23083f1bda1a60fe2b322c6cfab4a616af9edb`. Initial source reconnaissance also examined `be7ad2115583cf974ab9e2b95d706cdce880f0b9`; implementation decisions use the package commit. Its embeddable control avoids importing the comparable product's application or Windows Terminal settings UX.

| Option | Correctness evidence and limits | Licence / maintenance | Integration and portability |
|---|---|---|---|
| Devolutions managed engine + TermControl | Source tests, incremental VT/UTF-8, history, graphemes, alternate buffer, mouse, Skia/HarfBuzz; must run our own acceptance tests | MIT, recent source and package; young implementation, no assumption of iTerm2 parity | Native Avalonia control plus ConPTY and forkpty helper; lowest initial integration effort |
| XtermSharp | Existing VT engine and sample hosts; README explicitly lacks scrollback/reflow and grapheme clusters | MIT; [primary source](https://github.com/migueldeicaza/XtermSharp) | Avalonia rendering and platform transport adaptation still needed |
| Reuse iTerm2 VT/render stack directly | Rich behavior and regression suite | GPLv2-or-later / effective GPLv3 | Objective-C/Swift, AppKit/CoreText/Metal and server architecture: high rewrite effort, unavailable on Windows/Linux |

The selected NuGet package **does not bundle the Unix `dt-pty-host` executable**. Vendor only its pinned MIT C source and build it locally with clang/cc, link libutil on Linux. No unmanaged fork inside the managed process. Windows uses the package's ConPTY adapter. Evaluate native Ghostty/libvterm only if measured engine gaps justify another dependency; no product switch.

## Boundaries

`src/App`: Avalonia XAML window, session tabs/split layouts, focus, clipboard/search commands. `src/Terminal.Sessions`: platform factory, profiles and bounded iTerm2 protocol adapter. `native/unix-pty`: pinned forkpty host. `tests/Terminal.Tests`: engine regression tests, iTerm2 protocol tests, real PTY/process integration. Two production assemblies keep these boundaries explicit without proliferating projects. Profiles/settings persistence, safe opt-in bash/zsh launch wrappers, and shortcut resolution belong in their dedicated classes. Preferences edit a transaction and apply validated appearance changes to existing panes; shell changes affect new processes. A binary pane tree captures layout ratios and starts fresh shells on reopen.

Each pane owns one TermControl, one engine and one connection. The package processes output away from the UI thread, caps input queue at 4 MiB and posts invalidation to the dispatcher. UI close awaits asynchronous disposal of every session. Failed startup remains visible with its error; no simulated output. Independent sessions survive tab switching. Configuration restores fresh layouts; the separate local host architecture below additionally preserves live processes across UI loss when enabled. Normal close terminates sessions.

iTerm2-specific migration first targets OSC 133 exit-code parsing (including attributes) and OSC 1337 CurrentDir/RemoteHost metadata. Port the upstream parsing algorithm and translate only bounded approved metadata to the engine's supported protocol. Remote output is data; it cannot change profiles, steal focus, execute commands, access files or write clipboard. VT clipboard writes and notifications disabled by profile. Bracketed paste follows engine mode.

Milestone A: verified shell, tabs/panes, search/selection, profiles and keyboard navigation, three-OS runtime evidence, packages and contest documents. Milestone B: deeper tmux, replay, graphics, scripting and reconnect architecture. First acceptance: real PTY command, Unicode/formatting, resize/SIGWINCH, Ctrl-C, exit and clean close; GUI launch and screenshot supplement tests.


## Minimal desktop and screen replay, 0.3.0

The terminal fills the window by default. macOS commands are exported through
Avalonia NativeMenu; Windows/Linux retain a compact desktop menu. The tab strip
appears for multiple tabs, with an optional always-visible setting. A status bar
is optional, while errors remain visible in a dismissible overlay. A thin split
line has a wider transparent pointer target. Settings separate application,
profile General/Text/Colours, and shortcut editing.

Each pane records changed visible text screens at most four times per second,
with at most 256 frames and a 16 MiB conservative estimated retention budget.
Screens larger than half the budget are rejected, following the upstream DVR
capacity guard. This is a managed keyframe model, not a byte-for-byte DVR port.
Snapshot capture and output feed share an engine lock; storage lives in Sessions.
A replay viewer uses a separate fixed-grid engine without a connection. Control
layout/input/reset cannot mutate historical frames, and glyph sizing uses the
viewer render scale to handle Retina metrics. Seeking never
sends bytes or changes the live buffer. Closing a pane or disabling recording
clears its history. A viewer holds its frozen frame selection until closed.

Frames preserve cells, Unicode, text attributes, palette, grid size, cursor
position/visibility, reverse video and line rendition. Graphics and DRCS payloads,
per-frame cursor shape, binary DVR import/export and disk recording are outside
this subset. Recording a scrolled view captures that view; no full scrollback
history is stored in each frame. Fast intermediate output can occur between
captures. The budget is an estimate of retained managed objects, not a claim that
the process working set is capped at 16 MiB.

## Output rules and graphics acceptance, 0.4.0

Profiles hold up to 32 opt-in regex rules: highlight matched cells or bookmark
completed lines. Rules use .NET NonBacktracking (no lookarounds/backreferences),
100 ms regex timeouts and a 25 ms evaluation budget after the first rule per Feed. A timeout pauses the
rule; burst-budget exhaustion leaves a visible warning. At most the first 512
line-feed candidates per Feed and 4096 UTF-16 units per logical line are evaluated;
oversize/truncated lines are skipped. Alternate-screen output is excluded. No
script, command, send-text, clipboard, network or OS notification action exists.

The engine's existing lock covers capture/matching/colour application and stable
logical anchors. Plain lines copy only their current row; the view-relative
buffer API is briefly normalised under that lock, then restored without scroll
events. Wrapped lines need a retained-history snapshot to map Unicode positions
to cell ranges, so deep history has measurable allocation cost for these rules.
Rules run before line feed scrolls or evicts their cells. UI receives only a
coalesced change signal per Feed. Each pane keeps at most 200 line records,
clears them on close/reset, and resolves anchors again for navigation; expired
lines produce an explanation. This history is not a persistent logging service.

Existing inline graphics are now accepted through Aster's adapter: fragmented
sixel, OSC1337 PNG and direct-data kitty RGBA decode and render verified pixels.
Native smoke writes all three through a real shell and asserts red/green/blue
pixels in the native client render. Kitty file/shared-memory transfers remain
rejected by the pinned engine. This does not establish every image variant,
animation, graphics replay or upstream binary compatibility.

## Live local session hosts, 0.5.0 work

A recoverable pane owns its frontend connection; a separate headless C# host
owns the native PTY. Current-user-only .NET named pipes plus a random 256-bit
capability authenticate every connection. Descriptors live in the settings
folder's private sessions directory (0700, descriptor 0600 on Unix); Windows
uses the user's local application-data ACL and same-user/elevation pipe checks.
Descriptors contain identity/profile/capability, never output or environment.
There is no network listener. Only one frontend may attach at a time.

Explicit detach drains queued frontend input and, when advertised by the host,
waits for an ordered acknowledgement after native writes complete. Older hosts
without that capability retain their earlier best-effort detach behavior. Host
attachment cleanup removes output references under the output gate before queue
completion/cancellation disposal, so late PTY output stays in the journal.

Normal pane/window close sends explicit terminate and awaits owned host cleanup.
Lost IPC leaves the shell alive; Shell → Detach and close window requests that
behavior intentionally. Startup and Recover live sessions reconnect detached
hosts as tabs. Recovery keeps actual shell variables/jobs/process identities;
it does not merely reopen a command. Pane layout recovery remains independent.
New panes use hosts when KeepSessionsAlive is enabled; existing direct panes
cannot be converted in place. Detached hosts expire after 24 hours, and the
catalog caps descriptors at 64 with dead-host pruning.

Framed messages are at most 1 MiB; input chunks at most 32 KiB and queued input
at most 4 MiB / 128 frames. Slow frontends are detached rather than blocking PTY
output. The in-memory display journal is capped at 8 MiB raw frame data and 4096
frames, including ordered grid changes. Pixel dimensions travel with grid sizes so image anchors and window queries retain cell geometry. The host answers terminal queries;
frontends suppress protocol replies so replay does not send duplicate responses.
After journal overflow the host compacts to its real current text viewport;
recovery explicitly warns that older history/graphics/full terminal state may
need redraw. Shell state stays alive. This is not iTerm2's socket/server protocol
or full binary terminal-state serialization, and host OS restart is not survived.

Native acceptance deliberately exits a fixture UI process without Closing or
Dispose (exit 73), starts a new native UI, and verifies the same session/process,
shell-local variable, Unix shell PID, routed input, resize and final termination.
No user session or settings are used by this test. Final target-OS evidence is
required before support claims.

## Explicit native tmux control attach, 0.5.0 work

Shell → Attach tmux session opens a separate native window for an existing local
Unix tmux session. A Sessions controller launches `tmux -C attach-session` with
argument-list process startup and no shell interpolation. The original tmux
server owns all PTYs. Its response blocks and octal-escaped output feed bounded
parsing and a serialized command queue. Pane input is hexadecimal `send-keys`,
not text concatenated into tmux commands. Only explicitly requested UI actions
create/close/split/select tmux panes. Output does not execute host actions.

Windows map to native tabs; strict classic-layout trees map to native panes.
Existing pane controls survive rearrangement. The engine takes its grid from
ordered tmux layout messages, while control layout updates only cell metrics.
Native sizing accounts for per-pane padding and pixel dividers. Control clients have no rendered status rows.
Focus changes send a selection only when the active pane changes. tmux dimensions bound the client
resize; closing this window detaches and leaves shells alive. New panes bootstrap
from real `capture-pane` output and queried cursor state, then consume ordered
live notifications. The bootstrap is a text/colour viewport, not a full binary
terminal checkpoint: existing graphics/history and every terminal mode are not
reconstructed, and a busy pane's separately queried cursor can require redraw.
New interactive programs use the ordinary live VT path. There is no automatic
interception of arbitrary in-shell `-CC` DCS, remote/SSH/WSL attach or Windows
local tmux claim. tmux must be installed separately; it is not bundled.

Response blocks cap at 8 MiB; lines at 1 MiB; at most 64 windows/64 panes total, depth 16, 1000×500 with at most 100,000 cells per layout. Child geometry
must exactly partition its parent. Commands have a 10-second deadline; a timeout
detaches the client to preserve response correlation. Input is bounded at 4 MiB
and 128 queued chunks; byte commands are at most 512 bytes each. Unknown
notifications are ignored. No new package/native dependency is introduced.

## Opt-in system-wide summon window, 0.6.0 work

A fixed Control+Alt+Space chord registers with Carbon on macOS, RegisterHotKey
on a dedicated Windows message thread, or checked XCB passive grabs on X11.
There is no raw keyboard hook or arbitrary keystroke recording. Registration is
opt-in, can fail visibly on a collision, and is disposed on disable/exit. Linux
Wayland is explicitly unsupported until a portal-based implementation exists.
X11 handles Caps Lock / conventional Num Lock and suppresses autorepeat pairs.

The app service posts a registered chord to the UI dispatcher. It lazily creates
a dedicated MainWindow with real hosted sessions. An active window hides on the
next chord, preserving its panes/processes; another chord shows and focuses it.
Normal close still confirms/cleans up, and the next summon creates a fresh shell.
This window neither adopts detached normal sessions nor overwrites the normal
saved layout. Opened initialization runs once, including across hide/show.
Disabling the binding makes a hidden window accessible; an explicit Shell menu
also shows it. Upstream per-profile gestures, animated dropdown geometry and
complete multi-monitor/sleep/keyboard-layout parity remain separate work.

Native acceptance registers/unregisters real backends. macOS checks Carbon event
dispatch without changing the user's foreground. CI alone may launch an isolated
peer and inject a physical chord for cross-application summon, retained-variable
hide/show and cleanup. macOS Post Event access is preflighted; denied access is
reported as a skipped physical-focus test, never an invented pass. A separate
manual fixture can wait for a human physical chord without injecting one. Linux
CI uses Xvfb/Openbox and xdotool; these are test tools, not bundled runtime assets.

## Authenticated original Python wire core, 0.7.0 work

The opt-in Kestrel HTTP/1 WebSocket listener binds explicit IPv4 loopback and
requires the original local Origin/subprotocol plus a random 256-bit cookie.
A private endpoint file is independent of exported settings and output. A held
current-user file lease prevents competing servers replacing that credential.
Google.Protobuf handles the pinned original schema; Grpc.Tools generates C# only
during builds. ASP.NET Core 10.0.13 is included in self-contained payloads with
full notices. No web UI or external service is introduced.

Sessions owns transport/auth/framing/rate/queue limits and live-screen extraction.
The App target serializes script requests onto existing UI layout/session methods;
process and VT work stay in Sessions. Stable UI identities describe actual pane
trees. Bounded coalesced notifications report layout/focus/session changes.
Reading a live screen temporarily normalizes the engine buffer's viewport under
its existing lock, restoring the user's scroll offset without scroll events.

The bundled launcher uses the original Python client but replaces only connection
and authentication. It never discovers another iTerm2 or invokes its AppleScript
auth. Unsupported RPCs/ranges return explicit errors. Limits, supported operations,
trusted-script authority and coordinate bounds are in scripting.md. Native
acceptance invokes the original client against a uniquely owned endpoint/real
shells, checks Unicode/variable/input/layout/close and cleans up credentials/PTYs.

## Bounded scrollback Find, 0.8.0

Find captures at most one million cells / 20,000 newest physical rows under the
existing engine gate. Snapshot capture temporarily offsets the buffer without
scroll events and restores the user's view. Matching runs on a background task,
with query debounce/cancellation/generation checks so old completions cannot
replace newer queries or closed/pane-switched state. No process/VT work moves to
window logic; selection and clipboard stay independent of Find highlights.

The pinned MIT search mapping is adapted for NFC whole-grapheme boundaries and
wide-cell positions. Literal or .NET regex matches join soft wraps, stop at hard
lines and use logical-line cell anchors. Reflow/eviction/navigation resolves those
anchors; reset/alternate-buffer generations reject stale results. Visible overlays
use logical offsets and binary search within each matched logical line.

Literal case-insensitive remains default. Explicit Regex, Match case and Whole
word controls, arrows, Enter/Shift+Enter, Escape and counts fit a compact bar.
Invalid patterns stay visible. Queries cap at 1,024 UTF-16 units; logical lines at
65,536, total normalized text at 4 MiB and matches at 10,000. Regex uses a 25 ms
match timeout and the matcher checks a 300 ms overall budget. Partial output is
reported. These are bounded-work policies, not hard real-time latency guarantees.
Search is snapshot-based: typing/options or navigation refresh it; incoming output
marks the count stale until refresh. Regex syntax is .NET, not complete ICU parity;
zero-length/partial-grapheme matches and truncated logical prefixes are excluded.
Trailing default blank cells at hard EOL are omitted. tmux's existing literal Find
prompt remains separate; filtering/smart-case remain later parity targets.

## Graceful exit and scripting cancellation, 0.8.1

Transport request waits are bounded by their peer/server cancellation token,
independent of a desktop action's UI continuation. This prevents API shutdown
from waiting for the dispatcher while an Exit event is stopping that dispatcher.
Accepted actions retain their own cancellation checks and normal window/pane
cleanup; stopping transport does not roll back an action already in progress.

On macOS the Exit event arms a small AppKit bridge after hotkey/API cleanup.
It schedules a fresh main-queue turn, reissues NSApplication.stop and posts an
application-defined NSEvent to the same app. At most three 20 ms scheduled
attempts are made; scheduling is subject to OS load. Program.Main disarms it
when the native loop returns. This handles a reproduced case where Avalonia
12.1.4 returned from desktop.Shutdown but remained in NSApplication.run. No
process exit/kill, HID input, activation or other-app event is used. Cancelled
quit does not reach the Exit event. The bridge is isolated from terminal/VT code,
built with clang/AppKit on Mac only, and targets macOS 15.

Fixture evidence records PID, Python phases/output, desktop Exit and native-loop
return. The installed Mac verifier refuses reused report roots, requires the
launched process to exit, and requires scripting's clean main-loop marker.
Native tmux acceptance waits for the owned PTY's actual kernel geometry before
sending its one-shot stty command; layout metadata alone is not that barrier.

## Shared tmux Find, 0.9.0 work

Normal and explicit tmux windows use the same compact FindBar and existing
per-pane PaneFindController. The bar binds only the selected pane; pane/tab
changes deactivate old results, while hide/removal/close cancel pending work.
Find remains independent of terminal selection/input and tmux continues to own
its shells. Metadata refresh preserves Find textbox focus. Tab content bounds
changes schedule native tmux resizing, including the space consumed by Find.
New native tmux windows select their created tab. No VT/protocol/dependency or
search-bound changes. Two headless real-tmux desktop facts and final native Unix package checks
pass in source 78376ee / run 38906845116, including compact layout, Unicode,
grid fit and detach. Windows retains the common bar in normal panes; local
tmux itself remains unsupported there.

## Live tab ordering, 0.10.0 work

Normal-window tabs use an observable collection of existing TabItems. Reorder
moves that item and its workspace under the existing layout-mutation gate;
it never replaces panes or connections. The selected tab, focused Find editor,
active split and zoom remain attached to the same identities. Saved layouts
and scripting snapshots enumerate the reordered workspace list. Scripted tab
insertion shares the same move operation.

Mouse drags begin only on a tab header, excluding its close button, after a
six-pixel threshold. Capture belongs to the tab strip. An insertion marker
identifies the target edge; release outside a header, Escape, capture loss,
window deactivation, tab removal and close cancel the gesture. No external
drag/drop payload or shell input is involved. Keyboard/menu movement wraps at
the ends, matching pinned iTerm2's ungrouped moveCurrentTabByOffset behavior.
Groups, pinned tabs, transfer between windows, pane dragging and tmux server
window ordering are separate gaps. No new dependency or terminal change.

Final source 87184b0 / run 39001628564 passes 80 tests (including platform skips)
and native package acceptance on all three targets. The exact installed Mac
archive also passes the complete ordered 2× Retina batch with three scripting
repetitions and actual process/native-loop exits.

## Shared compact desktop chrome, 0.11.0 candidate

TerminalTabs is a TabControl with slots for the existing menu, Find and status
controls. Its template combines a 32-point caption/tab strip and selected
content; existing TabItems retain their panes/identities. Header sizing fills
available width with a 100-point minimum, then scrolls; selected tabs reveal
without moving keyboard focus. WindowButtons uses actual window close/state
APIs, and empty caption drags call BeginMoveDrag. BorderOnly decorations retain
native frame resizing. WindowCaption applies the same treatment to auxiliary
windows. No native input hook, forced process exit or terminal simulation.

Settings retains its transaction and named fields while changing category and
profile navigation visuals. Tmux listens to selected content size changes so
Find/layout changes still resize its PTYs. UI reference and platform limitations
are documented in visual-parity.md; exact native acceptance is required.
