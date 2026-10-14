# iTerm-style desktop and replay implementation plan

**Goal:** Match iTerm2's minimal terminal-first interface and advance the remaining application features with verified screen replay.

**Architecture:** Preserve real PTY/control/session boundaries. Native macOS menus share commands with desktop menus. Hide redundant chrome; maintain optional persisted status/tab settings. Capture bounded, timestamped terminal screen states separately from the live engine so reviewing history cannot send input or disturb a running process.

**Tech stack:** Existing pinned .NET 10.0.402, Avalonia 12.1.4 and Devolutions terminal component. No new runtime packages.

1. Inspect installed iTerm2's single session, tabs, splits, find and settings using native window captures. Inspect pinned DVR source for bounded recording/seek behavior. Preserve user sessions and preferences.
2. Remove permanent toolbar and pane borders/footers. Auto-hide a single tab, add compact tab styling and subtle split focus; put commands in the macOS native menu with a normal desktop fallback. Make status optional and errors visible independently.
3. Reorganize profile settings into General, Text, Colours and Keys sections; keep save/cancel validation and stable named controls. Add persisted chrome preferences and compact find.
4. Implement bounded in-memory screen replay, with timestamp seek and read-only viewer. Capture only when enabled, limit both bytes and frame count, release on close; never replay into the live engine. Record differences from original DVR binary format.
5. Run existing real PTY/UI suite plus replay/lifecycle acceptance. Capture native matched single/tabs/splits/find/preferences/replay dark/light states; inspect and repair visible defects.
6. Build verified self-contained packages on each target OS. Update migration/parity evidence and source, install the Mac update and publish the tested release. Full live process reconnect, native tmux -CC and Python API remain separate engineering targets; no unsupported parity claim.

The user has authorised implementation and local setup, opening iTerm2 and a minimal matching UI. Proceed serially without additional approval questions. Test behavior and lifecycle; avoid tests that merely restate styles.
