# Porting iTerm2 Workflows to Avalonia: A Real Terminal Across Three Desktops

*What we reused, what we rewrote, what failed, and what eight calendar days actually produced.*

The terminal screenshot is the easy part. A port is only real when the shell behind that screenshot is real, when resizing reaches the operating system's terminal, when closing a pane cleans up its child process, and when the same codebase can do those things on more than one desktop.

That was the bar for Aster Terminal, our C# and Avalonia migration of a documented subset of iTerm2 workflows. We started from an empty repository on 14 October 2026 and prepared release 0.14.0 on 22 October. Eight calendar days later, Aster shipped self-contained macOS, Windows, and Linux packages with native CI acceptance. It is a substantial port, not a complete iTerm2 replacement, and our screenshots do not prove pixel identity. This is the honest account of the migration for the [Avalonia Port Challenge](https://avaloniaui.net/blog/avalonia-port-challenge).

**Project:** [Aster Terminal on GitHub](https://github.com/kris70lesgo/aster-terminal) · [v0.14.0 downloads](https://github.com/kris70lesgo/aster-terminal/releases/tag/v0.14.0) · [verification matrix](https://github.com/kris70lesgo/aster-terminal/blob/main/docs/test-matrix.md)

## What the challenge asked us to show

The official challenge allows ports and rewrites of existing applications, provided the source application's licence permits it. For an open-source entry, the announcement asks for a public repository under an OSI-approved licence, downloadable builds for every targeted platform, before-and-after screenshots, and a migration write-up in the README that explains what the work actually cost. The write-up is mandatory and part of judging. It asks the practical questions: what translated cleanly, what needed rethinking, what surprised us, and how much work the migration took. AI-assisted code is allowed.

That requirement shaped how we reported the result. A polished screenshot could not stand in for a running terminal, a green build could not stand in for native runtime evidence, and an invented hourly estimate would be worse than an explicit “not measured.” Aster's README includes the required migration and cost account; this longer post adds the engineering detail.

## Starting from the actual problem

iTerm2 is not just a terminal drawing surface. Its macOS implementation combines Objective-C and Swift, AppKit window and responder behavior, Metal and CoreText rendering, a VT state machine, profiles, shell integration, PTY process management, and a long history of compatibility work. Replacing the window while keeping none of the process and terminal semantics would have produced a mockup, not a useful port.

The opposite choice—translating the whole application line by line—would have tied the work to Apple frameworks that do not exist on Windows and Linux. The upstream design also has responsibilities that deserve distinct boundaries in a portable application. We chose a narrower target: real local shells and the terminal workflows we could implement, package, and verify on three desktop systems within the challenge window.

We pinned iTerm2 source at commit [`069b6e0`](https://github.com/gnachman/iTerm2/tree/069b6e0dd8f3f0d82fc08ea5d294d28f7c5b986e) for study and parity tracking. We evaluated XtermSharp, a direct port of iTerm2's terminal stack, and an embeddable managed control. XtermSharp's documented gaps around grapheme handling and reflow mattered for a modern terminal. iTerm2's renderer and application shell were too closely coupled to the Apple stack for a short cross-platform rewrite. We selected Devolutions' MIT-licensed `Terminal.Control` engine, pinned its source, and kept its license notices.

This was a conscious split of responsibilities: use an existing managed VT engine and renderer as the terminal foundation, then implement Aster's desktop, sessions, profiles, platform process adapters, and selected iTerm2 compatibility around it. We did not claim the Devolutions engine was an iTerm2 translation.

## The architecture we ended up with

The Avalonia application owns the window, menus, tabs, split layout, focus, profile editing, and keyboard commands. A separate sessions layer owns the shell lifecycle, terminal profiles, persistence, and bounded iTerm2 protocol adaptation. A dedicated rendering assembly contains the attributed managed presentation-source adaptation used to tune rendering independently from process code. On Unix, a small pinned MIT C helper provides `forkpty`; Windows uses ConPTY through the terminal connection layer.

Each ordinary pane owns one terminal control, one engine, one connection, and one shell session. The pane is responsible for disposing its session. Output is read away from the UI thread; input and history are bounded; rendering updates are marshalled to Avalonia's dispatcher. A failed shell startup remains an error visible to the user. There is no simulated terminal text in the product path.

```text
┌──────────────────────────── Avalonia desktop UI ────────────────────────────┐
│ windows · menus · tabs · split tree · profiles · search · keyboard actions │
└───────────────────────────────┬─────────────────────────────────────────────┘
                                │ session commands / rendered state
┌───────────────────────────────▼─────────────────────────────────────────────┐
│ Session layer: profile + bounded iTerm protocol adapter + pane ownership   │
└──────────────────┬───────────────────────────────────────┬─────────────────┘
                   │ terminal connection                    │ recovery IPC
        ┌──────────▼──────────┐                  ┌───────────▼──────────────┐
        │ Pinned VT/control   │                  │ Optional local session  │
        │ engine + isolated   │                  │ host (same-user pipe)   │
        │ renderer adaptation │                  └───────────┬──────────────┘
        └──────────┬──────────┘                              │ owns live PTY
                   │                                          │
        ┌──────────▼─────────────────────────────────────────▼──────────────┐
        │ Platform process boundary: Unix forkpty helper / Windows ConPTY   │
        └───────────────────────────────────────────────────────────────────┘
```

In an ordinary session the pane owns its connection and shell. With live recovery enabled, the local host owns the PTY while the window is only a frontend. Tmux is a separate path: an existing tmux server owns the processes, and Aster attaches through its local control protocol.

That boundary proved more important than any particular control. UI code must not decide how a PTY is reaped. VT output must not become an instruction to the host operating system. An upstream shortcut must not silently become a clipboard write. The architecture keeps these concerns separate and gives tests somewhere concrete to assert them.

### Real processes on three platforms

On macOS and Linux, each shell runs through a Unix PTY and the pinned `dt-pty-host` helper. Windows shells use ConPTY. Resize changes the actual terminal grid; input reaches a real process; signals and close are checked against process exit. Tabs and splits have independent sessions.

The NuGet package we selected did not contain its Unix helper executable. Its connection code expected that helper at runtime, so a managed build alone could not reveal the packaging hole. We traced the package to its pinned MIT source, included only the required C helper, and compile it on the target Unix system with the system compiler. Linux also links `libutil`. This kept the native boundary small instead of placing an unmanaged `forkpty` implementation inside the managed process.

### Separate features instead of pretending they are one protocol

We adapted a bounded subset of iTerm2's OSC 133 exit-code parsing and OSC 1337 directory/host metadata. Remote terminal output stays data: it cannot run a host command, change profiles, take focus, access local files, write the host clipboard, or create notifications. Multiline paste asks before sending text. These are not incidental restrictions; terminal output can come from an untrusted remote host.

Recovery and tmux also retain their own ownership boundaries. With recovery enabled, a separate current-user local host owns a shell so that a UI crash or intentional detach need not kill it. A current-user authenticated local pipe connects the UI and host; the bounded descriptor contains identity and capability data, not shell output or environment variables. Normal pane close still terminates the shell. Aster's tmux integration is an explicit local `tmux -C` control attach: the tmux server owns those shells, while Aster presents the windows and panes as native controls. It is not automatic in-shell `tmux -CC` or remote SSH/WSL support.

The Python scripting API follows the same principle. The original `iterm2` 2.30 client can drive a tested core—creating windows, tabs, and panes, sending commands, reading Unicode viewport contents, observing layouts, and closing owned sessions—when the user explicitly enables it. It is disabled by default because scripts can control terminal sessions. This is not full API or AppleScript parity.

## How the feature set grew

The first slice established real PTYs, tabs and splits, profiles, search, resize, copy/paste, and deterministic cleanup. From there, features were added only with a test path and an explicit boundary:

- Bounded text-screen replay records visible terminal state in memory while the real shell continues. It is not iTerm2's binary DVR format and does not record all graphics or full scrollback.
- Profile output rules can highlight matched cells or bookmark completed lines. They are bounded and never execute scripts, send text, invoke clipboard actions, or notify the host.
- Inline-graphics tests cover sixel, iTerm2 OSC 1337 PNG, and direct-data kitty RGBA. Other variants, animation, file-transfer forms, and replay of graphics remain unverified.
- Native hotkey support can summon, hide, and reshow a terminal while keeping its shell alive. It is optional; Linux currently requires X11.
- Find gained bounded background literal/regex search, Unicode-aware anchors, result navigation, Smart Case as an option, and a shared workflow for the tested tmux pane subset.
- Tabs can be reordered while preserving their shell sessions. Panes can swap their live sessions while retaining their split geometry. Open Quickly fuzzy-matches live panes and configured profiles; smart double-click selection is optional.

For the 0.14.0 appearance pass, we compared the app against an installed iTerm2 3.7.4 reference. We aligned selected window geometry, caption and separator measurements, traffic-light centers, Monaco cell dimensions, terminal margins, the history gutter, and sampled colors. We adapted the pinned managed presentation source in an isolated assembly and adjusted Mac color management, text raster behavior, synthetic bold/italic, and emoji fallback. Existing user profiles are preserved; the reference appearance is an explicit transactional setting.

The result is closer in specific measured ways, but not pixel-identical. The public iTerm screenshot in the repository is an iTerm2 3.6.6 startup reference; the 3.7.4 frame measurements came from a separate installed reference. The Aster CI screenshots use controlled shell fixtures at 1×. They show actual applications and real terminal sessions, but they are not equal-state, equal-scale image pairs. We have not measured a defensible whole-window similarity score. The [visual report](https://github.com/kris70lesgo/aster-terminal/blob/main/docs/visual-parity.md) records exactly what was measured and what remains open.

## Before and after: the evidence we can stand behind

The original screenshot is an installed iTerm2 3.6.6 startup reference. The Aster capture comes from the v0.14.0 native macOS package running a real shell fixture. Different output, title, version, scale, and capture method mean this pair illustrates the port; it is not a pixel-diff result.

![Installed iTerm2 3.6.6 startup reference](https://raw.githubusercontent.com/kris70lesgo/aster-terminal/main/docs/evidence/iterm2-original.png)

*iTerm2 3.6.6 reference. The screenshot's shell content belongs to the original application and is not recreated as window chrome.*

![Aster Terminal 0.14.0 native package running a real shell fixture](https://raw.githubusercontent.com/kris70lesgo/aster-terminal/main/docs/evidence/v0.14.0-osx-arm64/dark.png)

*Aster 0.14.0, native macOS package. This controlled acceptance fixture checks real input, Unicode output, and rendering.*

![Aster Terminal 0.14.0 split panes](https://raw.githubusercontent.com/kris70lesgo/aster-terminal/main/docs/evidence/v0.14.0-osx-arm64/splits.png)

*Split-pane acceptance capture. The hosted capture uses a test theme and deliberately small shell fixture.*

## The failures taught us where the boundaries were

The early headless tests found a mismatch between Avalonia's headless test integration and the xUnit version in the package set. We resolved the test-framework boundary rather than dropping the integration tests. Those tests were useful, but they did not establish that a native window opened, so we added final-binary smoke runs with a real desktop backend and shell on every target. Linux runs with X11 under Xvfb; that is not a Wayland test.

Process cleanup was another lesson. On Linux, a background child could keep the PTY open after the shell exited. Reaping only the direct child was insufficient, so the helper now cleans up members of its own PTY session. During live recovery, detaching a frontend could also cancel an already-accepted ConPTY write and inadvertently end the supposedly persistent shell. The fix moved the lifetime of accepted input to the host, where the native process actually lives.

Rendering and disposal can race. The terminal library's asynchronous disposal could complete off the UI thread while a queued render still referenced the control. Closing the pane now detaches the view and drains queued rendering before disposing the session. The regression exercises a real nested layout and real shell rather than just checking that a close command returned.

The original Python client exposed a macOS shutdown stall that ordinary tests had missed: protocol and PTY cleanup finished, but the AppKit loop did not always return. We traced the phases and added a small Aster-authored AppKit stop/event bridge. We also found a separate transport-stop path that waited on an unfinished UI continuation; stopping the server now releases the listener and credential lease without waiting for that action. The final installed and hosted checks require both process exit and a clean native loop return for these fixtures. Historical failures are retained in the evidence instead of being rewritten as successes.

Tmux revealed a different class of bug. Control-mode responses can split escaped UTF-8 bytes across frames, and the server's grid dimensions must account for cell padding, dividers, and the compact Find bar. Treating each line as independent text or resizing only the Avalonia control produced corrupted output and clipped full-screen programs. The client now preserves raw bytes through framing, serializes commands, uses server-owned grid geometry, and waits for the actual PTY size in acceptance tests.

Visual comparison also needs instrumentation. A CI process inherited `NO_COLOR=1`, which changed the shell fixture and made a renderer comparison misleading. A window-state endpoint returned JPEG bytes under a `.png` filename. Color samples changed under the Mac display profile, and a one-sided padding assumption broke selection alignment. We switched to lossless captures, a clean comparison environment, measured glyph cells and padding at the actual display scale, and compared defined regions. We explicitly did not turn selected color matches into a claim that the whole UI is identical.

## What the final evidence says

The final runtime source is [`0d5ca38`](https://github.com/kris70lesgo/aster-terminal/commit/0d5ca3813703fce55fcdd8801e7c37918729ca74). GitHub Actions run [39748978754](https://github.com/kris70lesgo/aster-terminal/actions/runs/39748978754) ran 123 test cases on each target with zero failures: macOS and Linux each passed 121 with 2 Windows-only skips; Windows passed 112 with 11 Unix-only skips. All three self-contained release packages passed native window, PTY, resize, render, preferences, Find, tab/split, cleanup, hotkey, recovery, and Python-client acceptance. Unix also passed explicit tmux control-mode checks.

The platforms are not interchangeable, so we state the tested environments. macOS validation used Apple Silicon and macOS 15.7.9. Windows ran on hosted Windows Server 2025 / build 10.0.26100; Windows 11 is the intended desktop target, but this CI run is not a physical Windows 11 test. Linux ran on Ubuntu 24.04 x64 with X11/Xvfb. Wayland, wider physical DPI/monitor/IME matrices, physical pane-drag gestures, and the human-triggered macOS global hotkey remain gaps. macOS packages are ad-hoc signed but not Developer ID notarized; Windows packages are unsigned.

The archives and their SHA-256 sidecars are on the [v0.14.0 release page](https://github.com/kris70lesgo/aster-terminal/releases/tag/v0.14.0). The source repository retains exact CI reports, native screenshots, test results, dependency provenance, and payload/license audits. Build instructions and the full parity inventory live in the [README](https://github.com/kris70lesgo/aster-terminal#readme), [test matrix](https://github.com/kris70lesgo/aster-terminal/blob/main/docs/test-matrix.md), [architecture](https://github.com/kris70lesgo/aster-terminal/blob/main/docs/architecture.md), [feature inventory](https://github.com/kris70lesgo/aster-terminal/blob/main/docs/iterm2-feature-parity.md), and [licensing notes](https://github.com/kris70lesgo/aster-terminal/blob/main/docs/licensing.md).

## What this migration cost

The repository records an eight-calendar-day delivery span, from 14 October to 22 October 2026. We did not keep a timesheet, AI-credit ledger, or invoices for tool/cloud usage. That means hands-on engineering hours, AI spend, and total cash cost are **unmeasured**. Commit timestamps cannot reliably reconstruct them, so we will not convert them into a made-up estimate.

Tools included Codex, .NET 10.0.402, Avalonia 12.1.4, GitHub Actions, and local terminal tooling. The project adds no telemetry, cloud sync, or AI service at runtime. AI assistance was used; the challenge explicitly permits it.

There is a second cost that a single number cannot capture: the subset decision. A terminal has decades of edge cases. We chose to spend the available time on genuine sessions, observable behavior, release packaging, and reproducible tests, while writing down missing features rather than presenting a small demo as complete parity. The complete matrix is long because “not implemented” and “not verified” are different engineering states.

## What we would do next

If we continued beyond the challenge, the next investment would be a more rigorous equal-state screenshot harness across operating systems and display scales, followed by physical IME, monitor, window-manager, clipboard, and drag testing. On the feature side, automatic `tmux -CC`, richer scripting compatibility, terminal graphics/replay variants, and additional iTerm2 profile and trigger behavior are still separate work. We would measure each against a user-facing workflow before adding another dependency or claiming compatibility.

The useful lesson is that a desktop port is a chain of responsibilities, not a framework swap. Avalonia gave us one C# UI codebase across the three targets. It did not make Unix process cleanup, ConPTY lifecycle, AppKit shutdown, licensing, screenshot calibration, or the limits of an incomplete feature set disappear. The work became credible when each of those boundaries had an owner, a failure case, and evidence that the packaged application—not just the source—could cross it.

---

*Aster Terminal is an independent GPL-3.0-or-later port of a documented iTerm2 subset. The project has no iTerm2 endorsement. See the [public repository](https://github.com/kris70lesgo/aster-terminal) and the [official Avalonia Port Challenge announcement](https://avaloniaui.net/blog/avalonia-port-challenge).*
