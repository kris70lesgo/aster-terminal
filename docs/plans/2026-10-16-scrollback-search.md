# Scrollback Find Implementation Plan

**Goal:** Complete compact native Find with bounded background regex/literal search, case/whole-word options and wrapped Unicode navigation.

**Architecture:** Sessions captures a bounded immutable snapshot under the existing engine gate and searches off the dispatcher. Results use logical-line cell anchors, preserving positions through reflow and history changes. App manages cancellation, navigation and visible highlights; no PTY or VT behavior moves into a window. Reuse/adapt the pinned MIT TextBufferSearch cell-to-normalized-text mapping, preserving attribution and notices. No new dependency.

**Tech Stack:** .NET 10.0.402, Avalonia 12.1.4, pinned Devolutions engine, System.Text.RegularExpressions.

## Decisions

Keep literal case-insensitive as the existing default. Explicit Match case, Regex and Whole word controls accompany previous/next and a count. Enter/Shift+Enter navigate; Escape closes and returns input focus. Typing/options debounce before background search; cancellation/generation checks discard stale completions. Empty input clears. Bad regex and resource limits remain visible. Search output is data; it cannot write clipboard or trigger host actions.

Use the established engine and .NET regex rather than a separate regex engine or control fork. Regex has per-logical-line timeouts and a total budget; cells, text, logical-line length and result count are bounded. Report partial searches accurately. Regex operates on one logical line at a time, across soft wraps but not hard newlines. ICU-only syntax, smart case and filtering remain separate parity targets.

## Implementation

1. `src/Terminal.Sessions/Search`: immutable options/result/hit types, bounded logical-line search and Unicode/word/cell mapping. Add `ItermTerminalEngine` bounded capture and anchor navigation/visible projection under its gate; retain user scroll while capturing. Tests cover wide/combining/emoji, soft vs hard wraps, case/whole-word, invalid/zero-length/expensive patterns, caps/cancellation, resize/eviction/reset/alternate buffer.
2. App find controller: serialized background request with cancel/generation, anchored selected-hit preservation and wrapping previous/next. Highlight visible cells with current-hit distinction; output changes are refreshed on navigation/search. Dispose/cancel on hide, pane change or pane close. Keep existing terminal input and selection independent.
3. Compact Find controls in MainWindow XAML and a search partial; test actual PTY output, keyboard navigation, options/error states, independent panes, resize and cleanup. Keep tmux's existing literal prompt behavior explicitly scoped until the shared workflow is integrated.
4. Extend native final-package acceptance with real-shell regex/case/word/Unicode results, screenshot and cleanup. Run complete build/tests and native three-OS CI, inspect installed Retina package. Update architecture/parity/migration/test evidence and licensing/source attribution; audit payload/rules before publishing a passing release.
