# Migration report — competition release 0.7.0

Aster Terminal delivers the competition subset of an iTerm2 behavior migration:
real local terminals, profiles, tabs, nested panes, search, themes, persisted
configuration/layouts, original opt-in shell integration, bounded text-screen
replay, safe profile output rules, live local shell recovery and explicit native
Unix tmux control attach. Full upstream parity
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
   cover initial shell, tabs, splits, search, compact settings and replay on
   three OSes in 0.5.0 (final combined provenance in test-matrix.md). Local macOS acceptance also
   passes at 2× Retina scaling after repairing historical viewer reflow. The interface removes permanent chrome and exports
   macOS commands to the system menu. Screen replay uses bounded portable
   keyframes and an independent renderer; the live shell keeps running. Profile
   rules add matched-cell highlights/bookmarks with bounded history navigation.
   Sixel, OSC1337 PNG and direct-data kitty RGBA have adapter and native pixel
   acceptance, while broader graphics compatibility remains unverified.
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
   Current-user authenticated hosts preserve real shells across frontend loss;
   accepted PTY input uses host lifetime to avoid killing ConPTY on detach.
   Bounded tmux control framing preserves raw bytes between UTF-8 fragments;
   server-owned grids and pixel sizing prevent fullscreen pane clipping.
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
Milestone B includes automatic in-shell tmux -CC / remote attach, full binary/image DVR compatibility,
full trigger action/plist compatibility, full Python API parity, configurable hotkeys/Wayland and broader graphics/compatibility coverage.

0.6.0 adds an opt-in native global summon window: registered Carbon / Win32 /
XCB chord, dedicated real terminal, retained shell through hide/show, visible
conflicts and release on disable/exit. Final source c651f5b / run 38763544008
passes all three native packages including another-process foreground injection.
Local Retina package checks pass; the human chord attempt on macOS 26 remains
unverified. Verification also found/fixed a detach-output disposal race that could
end the supposedly persistent shell. The failure and 120-cycle regression evidence
are retained in migration-log.md. No new runtime dependency was introduced.

0.7.0 adds a disabled-by-default, authenticated original Python API core, using
pinned protobuf/WebSocket libraries and a bundled transport-only launcher. The
original iterm2 2.30 client now creates real windows/tabs/panes, sends commands,
reads Unicode viewport contents and closes owned sessions on all three platforms.
Six focused scripting tests and the full 66-test suite pass. Final source ef6d642 /
run 38792785139, payload/license audit and exact installed 2× Retina evidence are
recorded in test-matrix.md. The local test helper's virtualenv-path failure is
preserved separately. Unsupported APIs and historical coordinate limits remain
explicit in scripting.md; this is not a full AppleScript/iTerm2 API port.
