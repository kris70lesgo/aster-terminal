# Competition release completion

Continue from verified baseline 8665b31. User authorised full local setup and release work on 2026-10-14.

1. Finish profiles/preferences, atomic persistence/recovery, themes, shortcuts, menus, session switching/closing, multiwindow and layout restoration (fresh shells, explicitly not live reconnection).
2. Integrate shell hooks/command marks and navigation. Extend title, Unicode, mouse/key, hyperlink, tmux/less and process cleanup acceptance. Reuse established rendering/protocol components.
3. Test actual application flows, inspect native dark/light/settings/panes, repair defects, capture evidence.
4. Build self-contained macOS app bundle, Windows archive and Linux archive/desktop entry. Run Windows/Linux hosted runtime tests and GUI launch smoke tests; test macOS final package locally.
5. Preserve all required third-party notices and corresponding source references. Publish public GPL source and only passing release artifacts. Complete README migration report and dated rule checklist. Do not assert unsupported advanced iTerm2 parity or a submission that did not occur.

Advanced iTerm2 DVR, native tmux control-mode, live server reconnection and Python API remain separate long-term acceptance targets. The current milestone is a complete usable competition subset with explicit boundaries, not a claim of all upstream features.

Completion record (2026-10-14): profile/settings/layout/UI work, original shell
integration, real compatibility tests, native package acceptance, public source,
notice/source audit, screenshots and write-up are implemented. Final native
checks pass on macOS, Linux and Windows, including PowerShell host output.
Release archives are published only from successful native jobs. The form has
not been submitted; entrant identity and form-only declarations are unverified.
Full iTerm2 parity remains Milestone B as specified by the original request.

0.3.0 extends the completed subset with the minimal desktop/settings design and
bounded text keyframe replay. Final native three-platform run 38693452893 passes.
Full binary/image DVR compatibility remains a separate target.

0.4.0 continues Milestone B with safe profile output rules (highlight/bookmark),
bounded line history and navigation, verified inline image subsets, and a fix
for incoming output moving a scrolled view. The 48-test suite and expanded
native smoke cover these additions. Full trigger action/plist compatibility,
reconnect, native tmux -CC, Python API and global hotkeys remain separate targets.

0.5.0 continues with authenticated current-user session hosts (same live shell
after UI loss/explicit detach) and native explicit local Unix tmux control
attach. Final three-OS package acceptance includes crash/reopen; Unix native
checks additionally verify independent nested tmux panes, kernel dimensions,
exact grid fit and same-shell reattachment. Automatic in-shell -CC, remote
attach, complete terminal checkpoint/DVR compatibility, Python API and global
hotkeys remain separate long-term targets.
