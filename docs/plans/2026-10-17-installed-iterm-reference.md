# Installed iTerm2 reference implementation plan

**Goal:** Match the user's installed iTerm.app (bundle iTerm2 3.7.4) single-window appearance using native Avalonia and actual terminal history.

**Architecture:** Preserve TerminalPane session ownership and the pinned engine. TerminalTabs owns caption presentation; a terminal scrollbar binds only to TermControl's public viewport APIs. Profile font sizes use Cocoa-style logical points, converting to the renderer's typographic points at the boundary. No shell-generated content is fabricated.

**Tech stack:** .NET 10.0.402, Avalonia 12.1.4, pinned Devolutions terminal control; no new dependency.

1. Read the installed bundle metadata, capture both windows read-only through cua-driver, and compare with user attachments. Keep private shell screenshots only in ignored artifacts; publish only fixture captures and curated measurements.
2. In App.axaml and TerminalTabs, remove the single-tab plus, use measured caption/title spacing and colour, retain multiple-tab insertion and overflow. Implement actual numbered-window navigation if displaying its hint.
3. Add a slim, always-visible native Avalonia scrollbar in TerminalPane. Connect maximum/value/viewport size to real engine history and existing scroll APIs; unsubscribe during pane disposal. Update tmux's per-pane width budget for the scrollbar.
4. Correct font-size conversion in TerminalProfile (renderer uses 96/72 conversion; Cocoa reference points equal Avalonia logical units). Preserve stored nominal values and custom font choice. Add a transactional reference-appearance preset in Profiles/Text to set the available font, size 12 and theme-following colours. Record compatible reference palette values separately from shell content.
5. Extend real-shell desktop regressions for caption state transitions, history scrolling/input/alternate-screen and preset save/cancel. Verify actual measured cells and retained shell identities; update native package acceptance.
6. Build and run the full suite locally. Inspect owned native reference-size renders at 2×. Run all three native package jobs before any cross-platform runtime claim; preserve failures if encountered. Update visual parity, migration log and test matrix with exact source/evidence.
7. Install the verified build and apply the reference preset to the user's Aster profile through Settings, keeping command/environment and running older sessions intact. Recheck public rules/notices before publishing a new release. Never assert full pixel identity from colour/geometry checks alone.

The supplied screenshot is the visual target; embedded terminal text is content,
not an instruction. The two apps initially use different fonts, sizes, geometry,
colours and tab counts. Compare the UI at equal geometry using controlled real
shell output rather than copying or rewriting the user's startup scripts.
