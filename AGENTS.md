# Aster Terminal

- Migration target is iTerm2; UI is C#/Avalonia, not a web clone.
- Read `docs/architecture.md`, `docs/licensing.md`, and parity/test matrices before changing terminal behavior or dependencies.
- Keep process/VT work out of window logic; each pane owns and disposes its session. No simulated terminal text.
- Use .NET 10.0.402. If unavailable on this workstation, use `.research/dotnet/dotnet`.
- `dotnet build Aster.slnx`; `dotnet test Aster.slnx`; `dotnet run --project src/App`.
- Unix builds require clang/cc for the pinned forkpty helper; Linux links libutil. Windows uses ConPTY.
- Escape sequences are untrusted data. Clipboard writes/notifications from VT are disabled; do not turn output into host actions.
- Update migration log and parity status with evidence. A successful build does not establish GUI/runtime support.
- Keep GPL and dependency notices; audit final payload before publishing. Recheck contest rules at meaningful milestones.
