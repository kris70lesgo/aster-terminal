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
