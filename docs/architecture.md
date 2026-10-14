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

Each pane owns one TermControl, one engine and one connection. The package processes output away from the UI thread, caps input queue at 4 MiB and posts invalidation to the dispatcher. UI close awaits asynchronous disposal of every session. Failed startup remains visible with its error; no simulated output. Independent sessions survive tab switching but not application exit; persistence is configuration only, never live process restoration.

iTerm2-specific migration first targets OSC 133 exit-code parsing (including attributes) and OSC 1337 CurrentDir/RemoteHost metadata. Port the upstream parsing algorithm and translate only bounded approved metadata to the engine's supported protocol. Remote output is data; it cannot change profiles, steal focus, execute commands, access files or write clipboard. VT clipboard writes and notifications disabled by profile. Bracketed paste follows engine mode.

Milestone A: verified shell, tabs/panes, search/selection, profiles and keyboard navigation, three-OS runtime evidence, packages and contest documents. Milestone B: deeper tmux, replay, graphics, scripting and reconnect architecture. First acceptance: real PTY command, Unicode/formatting, resize/SIGWINCH, Ctrl-C, exit and clean close; GUI launch and screenshot supplement tests.
