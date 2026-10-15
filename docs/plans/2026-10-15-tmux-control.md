# Native tmux control client implementation plan

Goal: attach to an existing local tmux session using its real control protocol,
map windows/layouts/panes to Avalonia controls, route independent input and size,
and detach without killing tmux's shells.

Architecture: a Sessions controller owns an explicit `tmux -C attach-session`
process with redirected streams. It parses bounded response blocks and escaped
output, serializes commands and sends key bytes as hex (no command interpolation).
A strict bounded layout parser consumes tmux's classic layout grammar. Separate
pane connections feed the existing VT engine; a native window keeps controls
across layout changes. Output never becomes a host action. No dependency is added.

Compared approaches: intercept arbitrary `-CC` DCS inside every local/SSH PTY;
explicit control attach; ordinary tmux only. Explicit attach isolates protocol
ownership and cleanup and provides usable native panes now. Automatic in-shell
`-CC`, remote control attach, perfect historical mode/graphics hydration and full
upstream tmux gateway compatibility remain separate work; do not claim them.
Pinned iTerm2 TmuxGateway.m and VT100TmuxParser.m informed protocol study;
local tmux manual (CONTROL MODE, capture-pane, refresh-client) is the protocol
reference. No upstream code is copied.

1. Add response/output parsing, bounds, command correlation, process cleanup,
   pane IO, window snapshots and strict layout parser in Terminal.Sessions/Tmux.
2. Allow TerminalPane to use an injected real connection. Add a native tmux
   window, explicit attach menu, native new window/split/close-pane/detach actions.
3. Verify malformed/fragmented escaped output and layouts. Use unique local tmux
   servers for real control-mode output, independent input, layout changes,
   Unicode, resize, detach/reattach retaining a shell-local variable and cleanup.
4. Verify native UI against actual tmux panes; add platform evidence accurately.
   Windows may run tmux through a future remote/WSL adapter; local Unix only now.
