# Competition release completion

Continue from verified baseline 8665b31. User authorised full local setup and release work on 2026-10-14.

1. Finish profiles/preferences, atomic persistence/recovery, themes, shortcuts, menus, session switching/closing, multiwindow and layout restoration (fresh shells, explicitly not live reconnection).
2. Integrate shell hooks/command marks and navigation. Extend title, Unicode, mouse/key, hyperlink, tmux/less and process cleanup acceptance. Reuse established rendering/protocol components.
3. Test actual application flows, inspect native dark/light/settings/panes, repair defects, capture evidence.
4. Build self-contained macOS app bundle, Windows archive and Linux archive/desktop entry. Run Windows/Linux hosted runtime tests and GUI launch smoke tests; test macOS final package locally.
5. Preserve all required third-party notices and corresponding source references. Publish public GPL source and only passing release artifacts. Complete README migration report and dated rule checklist. Do not assert unsupported advanced iTerm2 parity or a submission that did not occur.

Advanced iTerm2 DVR, native tmux control-mode, live server reconnection and Python API remain separate long-term acceptance targets. The current milestone is a complete usable competition subset with explicit boundaries, not a claim of all upstream features.
