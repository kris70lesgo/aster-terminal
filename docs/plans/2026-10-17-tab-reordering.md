# Live tab reordering

Continue the user's authorised iTerm2 Avalonia port. Reference: pinned iTerm2
`069b6e0`, `sources/TerminalView/PseudoTerminal.m`, `moveCurrentTabByOffset:`
and `moveTabLeft:` / `moveTabRight:`. Ungrouped keyboard movement wraps at the
ends. Preserve this behavior; groups, pinning, cross-window transfer and tmux
server window ordering are outside this change.

Keep the existing minimal strip. Add drag insertion markers, Escape/outside
release cancellation, and left/right menu commands with editable shortcuts.
Use a collection move of existing TabItems, not replacement shells. Serialize
order changes with other layout mutations. Keep selection, active pane, zoom,
Find focus, script identities and persisted layout order consistent.

Verify with routed pointer/key events and actual independent shells: both drag
directions, wraparound, cancellation, close-button exclusion, split/zoom and
Find preservation, saved order, scripting order and process cleanup. Extend
native package acceptance to check keyboard reordering and retained shell state.
Run the full suite and native CI on Windows/Linux/macOS before release claims.

Also remove hardcoded About/package version drift: the current About dialog
still reports 0.8.1 in a 0.9.0 build. Derive version labels from the project.
No new dependency or terminal protocol change.
