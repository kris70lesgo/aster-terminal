# First-execution evidence

Captured 2026-10-14 with cua-driver window capture, inspected locally. Native screenshots are real application output, not designs. Aster screenshots show actual user zsh startup; terminal tools shown are shell configuration, not Aster features.

- `iterm2-original.png`: installed iTerm2 **3.6.6**, copyright George Nachman/contributors, local startup reference. It is a different version from the pinned source audit and a smaller window; do not imply identical builds or geometry.
- `aster-initial.png`: first running Avalonia window with a live zsh prompt.
- `aster-final.png`: final developer publish after setting the native application name and reducing tab text size; captures real startup output, not an input acceptance test.
- `aster-tabs.png`: native Cmd+T produced a second session tab.
- `aster-splits.png`: native Cmd+D and Cmd+Shift+D produced three panes.
- `aster-search.png`: native Cmd+F opened the search bar; matching functionality is tested separately.
- `aster-pane-close.png`: native Cmd+W closed one pane and collapsed its split.
- `aster-native-command.png`: per-pid native key events typed `pwd` and Return, and the actual zsh output appeared in the selected split pane. The first bulk AX text attempts were no-ops; separate key events succeeded after pane focus and a later render capture.
- `first-slice.trx`: versioned latest automated test run. Tests named EngineTests, PtyTests and DesktopTests.
- `iterm2-COPYING.txt`: upstream pinned grant explaining GPLv2-or-later/effective GPLv3.
- `dependency-licenses.json`: package metadata inventory; not a complete native-payload audit.

Intermediate driver captures where typing produced no result have no successful-input evidentiary value. Final competition screenshots still need identical terminal commands/geometry, clean profiles, light/dark and preference states. Current screenshots must not be used as proof of full feature parity or cross-platform runtime support.
