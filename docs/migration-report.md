# Migration report — competition release 0.2.0

Aster Terminal delivers the competition subset of an iTerm2 behavior migration:
real local terminals, profiles, tabs, nested panes, search, themes, persisted
configuration/layouts and original opt-in shell integration. Full upstream parity
remains Milestone B. The README contains the mandatory public write-up.

1. **Difficulty/completeness:** iTerm2 combines Objective-C/Swift VT state,
   AppKit/Metal drawing and persistent native process servers. Aster rewrites
   desktop/session boundaries, directly adapts the OSC133 exit-code scan and
   OSC1337 metadata, and bundles original bash/zsh integration. It reuses the
   MIT Devolutions parser/render/control rather than pretending that those are
   translations of iTerm2's Metal code. The parity matrix records advanced gaps.
2. **Application quality:** genuine shells, independent tabs/panes, close
   confirmation, focus/zoom, save scrollback, find, copy/paste, profiles, actual
   font/colour updates and dark/light/system themes. Preferences are transactional;
   malformed settings recover with preserved originals. Native rendered snapshots
   cover initial shell, tabs, splits, search and preferences on three OSes.
3. **Cross-platform implementation:** one Avalonia/C# host; ConPTY for Windows
   cmd/PowerShell and a compiled MIT forkpty helper on macOS/Linux. All targeted
   packages launch and exercise their native backend on their own OS in CI.
   Linux is verified through X11/Xvfb; physical Windows 11/Wayland/multi-monitor
   coverage is not substituted by hosted tests. See test-matrix.md.
4. **Engineering quality:** pinned SDK/packages/upstream commits, lockfiles,
   atomic persistence, validation, bounded queues/history/control strings,
   serialized layout changes, real child cleanup and final-binary smoke tests.
   Linux cleanup handles background process groups in the PTY's own session.
   Terminal controls detach and drain pending UI rendering before async disposal.
   Test regressions exercise failures as well as success. Performance numbers
   describe actual workloads and expose the expense of full-history snapshots.
5. **Usefulness/costs:** migration-log.md records concrete failures/repairs.
   Human hours and AI credit costs were not tracked and remain unmeasured.
   Public GPL source/build scripts and full dependency notices accompany binaries.
   Upstream GPL mismatch is resolved from the explicit COPYING grant; exact
   dependency sources and native licence alternatives are documented separately.

Milestone A release deliverables: source, runnable three-platform archives with
checksums, before/after evidence, tests and write-up. Entrant declarations and
contest form submission remain separate human steps; no submission is asserted.
Milestone B includes reconnectable processes, native tmux -CC, iTerm2 DVR,
triggers, Python API, global hotkeys and broader graphics/compatibility coverage.
