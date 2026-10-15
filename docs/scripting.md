# Python scripting subset

Enable **Allow trusted Python scripts to control Aster** in Preferences → General.
The server is off by default. Scripts have authority to read terminal contents,
send commands and close sessions; run scripts you trust. The API listens only
on IPv4 loopback, default port 1912. A private current-user credential is created
in `api/endpoint.json` beside settings, rotated on restart and removed on stop.
Only one API server may own that settings directory. Key conflicts, occupied ports
and startup failures appear in the app's error area.

Use Python 3.11 or newer. Install the pinned original client and its dependencies
in your own virtual environment, then run an ordinary iTerm2 script through the
bundled Aster launcher:

```sh
python -m venv .venv
# macOS/Linux:
.venv/bin/python -m pip install -r Scripting/requirements.txt
.venv/bin/python Scripting/aster_script.py my_script.py
# Windows uses .venv\Scripts\python.exe instead.
```

`Scripting` is beside Aster.Terminal.exe / the Linux executable, or inside
`Aster Terminal.app/Contents/MacOS` on macOS. From source, use `scripts/python`.
The launcher finds Aster's own private endpoint, including a custom configured
port. `--endpoint PATH` selects a specific owned Aster settings directory.
It adapts only the original client's connection/authentication: it never probes
an iTerm2 socket, borrows its cookie, or invokes AppleScript authentication.
The upstream client's RPCs, session/window objects and notification dispatcher
remain in use. This is not an AppleScript or full API compatibility claim.

```python
import iterm2
import sys

async def main(connection):
    app = await iterm2.async_get_app(connection)
    window = app.current_terminal_window
    tab = await window.async_create_tab()
    pane = await tab.current_session.async_split_pane(vertical=True)
    await pane.async_send_text("echo hello" + ("\r" if sys.platform == "win32" else "\n"))

iterm2.run_until_complete(main)
```

| RPC / behavior | Scope |
|---|---|
| List sessions | Window/tab/pane IDs stable for the current UI lifetime, split hierarchy, actual grid sizes and titles; ordinary MainWindow instances only |
| Create tab/window | Default or named profile; per-session Command, Working Directory, Name and Custom Command=Yes; tab index / background tab in an existing window |
| Split | Vertical/horizontal, before/after, default/named/custom profile |
| Send text | Specific session, `active` or `all`; actual input, including control characters; Aster has no broadcast domains |
| Read buffer | Live viewport text, wide/combining cell mapping, wrap flags, cursor and basic SGR/colour styles; leaves the user's scrolled view unchanged |
| Close | Session/tab IDs, force or configured close confirmation; window-close RPC is explicitly unsupported |
| Activate | Window/tab/session selection and requested window activation |
| Profiles | Read Guid, Name, Command and Working Directory only |
| Notifications | New/terminated sessions, layout and focus, coalesced at 200 ms; no raw keystroke hooks |
| Broadcast domains | Empty list; no broadcasting is implemented |

Viewport coordinates begin at zero. Absolute historical buffer indexing after
scrollback eviction/reflow, trailing-line reads, graphics/style payloads beyond
the listed subset, tmux-controller windows, transactions, property mutation,
variables, RPC registration, UI tools, coprocesses and other RPCs return explicit
errors. Newer protocol features are not advertised. Closing the last app window
normally exits the app and stops its API; scripts should preserve a window if
they need to continue. Aster limits scripting to 16 windows / 64 live panes,
4 connected clients, 256 KiB requests, 1 MiB responses, bounded outgoing queues
and a request rate of 32/s (burst 64). Slow clients are disconnected.

Acceptance uses iterm2 2.30 / protobuf 7.36.2 / websockets 17.2. The original
client passes real native separate-window creation/close, session listing,
tab/split creation, shell-local variable + Unicode output reads, updated layout
and owned-session close on macOS, Windows and Linux in final run 38792785139.
Six focused C# tests additionally cover cookie/Origin/subprotocol refusal,
private credentials, exclusive ownership/restart, malformed/oversize frames,
subscriptions, desktop operations, scroll preservation and preferences/exit
lifecycle. Final packages and the exact installed Mac archive at 2× Retina pass;
provenance and explicit remaining limits are in test-matrix.md.
