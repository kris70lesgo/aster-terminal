# iTerm2 visual alignment

Target: the attributed iTerm2 3.6.6 dark reference in
[evidence/iterm2-original.png](evidence/iterm2-original.png), supplemented by
pinned source 069b6e0 (PSMMinimalTabStyle.m, PSMTahoeTabStyle.swift and
PreferencePanel.xib) and [official Appearance documentation](https://iterm2.com/documentation-preferences-appearance.html).
The capture and source versions differ. This is a source-referenced native
Avalonia adaptation; no pixel-identical/full UI parity assertion is made.

| Surface | Implementation / evidence at source checkpoint |
|---|---|
| Compact caption | Shared 32-point header, genuine close/minimize/maximize actions; border-only native frame retains resize. Caption drag/double-click use Window APIs. Local desktop action tests pass; physical OS drag/snap remains manual coverage. |
| Dark colours | Sampled reference header #21212D, terminal #15181D and inactive controls #444450. Applied to shared chrome and default terminal scheme; explicit user profile colours remain effective. No terminal font or user shell configuration change. |
| Tabs | Close before centred text, selected pill, fill available width, overflow scroll and selected-tab reveal; actual new-tab button. Existing reorder/cancel/Find/session tests retained. |
| Settings | General/Appearance/Profiles/Keys icon toolbar; profile sidebar and General/Colors/Text/Triggers segments; compact fields. Only functional categories are shown. Transactional save/cancel unchanged. |
| Other windows | Shared caption for tmux, replay, prompts and triggered-line history. Compact Find controls. tmux recalculates its kernel grid from the selected content's size, including Find height. |
| Platform differences | Window-manager shadows/corners, OS text rasterization, available fonts, taskbar/Dock and system menu behavior vary. Proprietary macOS fonts/compositor effects are not bundled or simulated. |
| Wider iTerm2 settings | Unimplemented functionality and its settings pages remain absent, per the feature matrix. This pass cannot establish full upstream UI parity. |

Local headless screenshots in ignored artifacts/ui-design-candidate show the
rendered controls, not native window-manager decorations. New tests exercise
caption actions with real independent shells, three-tab fill at 620 width,
crowded-tab scrolling, minimum-size Settings navigation and focus, plus cleanup.
Native Windows/Linux/macOS package acceptance and installed Retina captures are
required before final release claims; currently pending at this checkpoint.

No upstream image/font/icon asset was imported. Toolbar and caption icons are
small original vector paths. Existing GPL/MIT notices and corresponding source
obligations continue to apply; no new dependency was added.
