# Output triggers and graphics acceptance implementation plan

Goal: add usable profile output rules and establish tested inline graphics support.
Architecture: the Sessions engine evaluates completed logical lines at line feed through the existing VT dispatch under its existing lock. Plain lines copy only the current row; wrapped lines map retained history. UI change signals coalesce once per Feed. Regexes use .NET's bounded nonbacktracking engine, explicit timeout, bounded rule/text/match retention. Highlighting uses buffer coordinates; bookmarks use stable logical image anchors and stay in memory per session. No command, send-text, clipboard or notification actions. Profiles persist rules transactionally. The minimal terminal exposes a small history button only after a rule fires.
Tech stack: existing .NET 10 / Avalonia / Devolutions packages; no dependencies.

1. Add trigger settings and validation; tests for malformed/unsupported patterns, bounded input and persistence.
2. Add engine processing for completed logical lines, matching cell ranges including wide/combining glyphs, highlighting and bounded bookmarks. Test fragmented VT/UTF-8, wrapped/scrollback lines, alternate screen exclusion, repeat/reset/resize/eviction.
3. Add profile rule editor with Add/Remove, enabled/action/pattern/colours, and session history navigation; test save/cancel/invalid rules and a real shell producing a marked/highlighted line.
4. Verify existing OSC1337/sixel/kitty decoding and actual renderer pixels; extend native smoke with real shell graphics and trigger output/settings captures.
5. Build/test local and three-OS CI, inspect native Retina captures, install verified update and document scoped gaps. Recheck rules, audit notices and final packages before release.

Source references: pinned iTerm2 Trigger.m line/partial gating, HighlightTrigger.m captured-range mapping and MarkTrigger.m save-line behavior. This implements a safe portable subset, not all upstream regex/actions. Full reconnect/tmux control/API/global hotkeys remain separate work.
