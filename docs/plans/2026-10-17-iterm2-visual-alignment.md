# iTerm2 visual alignment

User requests the real iTerm2 UI. Reference: existing attributed
docs/evidence/iterm2-original.png (3.6.6, dark, Monaco 12), pinned upstream
069b6e0 PSMMinimalTabStyle / PSMTahoeTabStyle, PreferencePanel.xib, and official
Appearance documentation (checked 2026-10-17). No reference-app automation:
earlier native access was denied, so use the authorised existing capture/source.

Replace the extra native caption/tab layers with a shared compact Avalonia
header and real close/minimize/maximize controls. Preserve window movement,
resize and confirmation. Stretch tab headers across available space, place
close before the centred title, and add the functional new-tab button. Single
tabs show only their title, preserving the existing hide-tab-strip preference.
Use the same shell for normal and explicit tmux windows. OS shadows, text
rasterization and compositing may differ; do not claim pixel identity.

Replace Fluent selection underlines in Settings with an icon toolbar and compact
segmented profile tabs, keeping only functional categories. Preserve transactional
save/cancel and all existing fields. Use neutral iTerm-like dark/light tokens,
13-point UI labels, hairline dividers and compact native-style fields/buttons.
Keep terminal font/profile colours and real PTY behavior intact.

Verify real-shell tab selection/reordering/new/close, compact and overflow
headers, Find focus, window controls and settings navigation at minimum size.
Inspect dark/light native renders on target platforms. Existing native fixtures
must still verify exact terminal/tmux geometry, lifecycle and API behavior.
