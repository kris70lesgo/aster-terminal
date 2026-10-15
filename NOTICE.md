# Aster Terminal notices

Independent iTerm2 migration; no upstream endorsement. Aster-authored and
adapted code is GPL-3.0-or-later. See LICENSE. No warranty.

iTerm2 copyright George Nachman and contributors. Semantic exit-code scanning
and OSC metadata adapt VT100Terminal.m at
069b6e0dd8f3f0d82fc08ea5d294d28f7c5b986e (GPLv2-or-later, elect GPLv3).
The bounded screen replay design studies DVR.m/DVRBuffer.m at that same
commit (copyright George Nachman, GPLv2-or-later, elect GPLv3): capacity,
oldest-frame eviction and rejection of frames over half the budget. The C#
keyframe implementation is new and does not read iTerm2 binary DVR files.
Live host design studies the same pinned iTerm2 task/server adoption and socket
handshake implementation. The C# named-pipe host is a new implementation; no
upstream server code or binary is copied.
Output rule design studies Trigger.m, HighlightTrigger.m and MarkTrigger.m at
the same pinned source. Its C# regex/highlight/bookmark implementation is new;
no host command or clipboard trigger actions are enabled.
Bundled bash/zsh integration comes from iTerm2-shell-integration commit
bcd52317a70f1a489b86fcfd1ee97df155741092 under the same grant.
Original headers and included bash-preexec MIT notice are retained.
The zsh TERM_PROGRAM gate was adapted for Aster Terminal on 2026-10-14.

Devolutions Terminal copyright Devolutions Inc., portions copyright Microsoft.
Its terminal control and Unix helper are MIT. The helper was modified on
2026-10-14 to terminate Linux PTY session members on close. Source and licence
are included in the public repository. Embedded Cascadia Mono and Noto Color
Emoji remain OFL-1.1; full notices are under licenses/.

Full Avalonia, MicroCom, Tmds.DBus, Skia, SkiaSharp, HarfBuzzSharp, ANGLE,
ConPTY and .NET runtime notices are included under licenses/. Native third-party
notices are retained intact, including optional components not enabled by the
pinned build. See LICENSING.md and DEPENDENCY-SOURCES.md in the release,
or docs/licensing.md and docs/dependency-source-map.md in source.

Complete Aster source and build scripts:
https://github.com/kris70lesgo/aster-terminal

The explicit tmux control client studies iTerm2 TmuxGateway.m and
VT100TmuxParser.m at the same pinned source. Independent C# implementation;
no tmux source or executable is included.

Native summon hotkeys study iTerm2 iTermCarbonHotKeyController.m at the pinned
source and platform SDK APIs. Independent C# registration/window lifecycle code;
no upstream hotkey classes or application assets are copied.
