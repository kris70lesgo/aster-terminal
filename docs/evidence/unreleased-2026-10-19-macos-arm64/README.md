# Unreleased working-tree verification — 2026-10-19

This evidence covers pane swapping, opt-in Smart Case search, and iTerm-style
login-shell captions in the unreleased working tree. The source base is commit
`51d9488fec25b9fe83afe407d3e94421365211e8` with local changes; these changes
are not present in the published `v0.13.0` release. The local package kept the
project's `0.13.0` version metadata for testing and is not a submission build.

The project was built with the pinned `.research/dotnet/dotnet` SDK on macOS
26.7.2 Arm64. `dotnet build Aster.slnx --no-restore` completed with zero
warnings and zero errors. `dotnet test Aster.slnx --no-restore` completed with
108 passed, 0 failed, and 2 Windows-only ConPTY tests skipped (110 total).
The saved [TRX results](acceptance.trx) include the regression tests for
Unicode-aware Smart Case in normal and tmux Find, and live-shell pane swaps
across nested and shared split layouts, plus login-shell title formatting that
preserves custom profile names.

The self-contained `osx-arm64` package smoke passed against real macOS windows,
PTYs, and shells. The
[native smoke report](native-smoke/native-smoke.json) covers shell output,
resize, screen replay, triggers, image protocols, tab movement, Find, native
preferences, and cleanup. Separate reports record passing Python scripting,
session recovery, and tmux checks. The optional physical cross-app hotkey test
was skipped; the automated Carbon registration/dispatch/unregister check
passed.

The tested local archive was
`artifacts/packages/aster-terminal-0.13.0-osx-arm64.zip`, SHA-256
`5f238fa9754b1957485a6482f5241b611bb6a44a14d332c1c52a5e771ef9657f`.
That archive is a local test artifact and is not included here or published.

Windows and Linux builds and runtime checks for these working-tree changes are
still pending. The published release remains unchanged. See the project
[test matrix](../../test-matrix.md), [feature parity matrix](../../iterm2-feature-parity.md),
and [migration log](../../migration-log.md) for the current status.
