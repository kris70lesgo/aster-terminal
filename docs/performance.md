# Measured performance — 2026-10-15

## Release 0.3.0

Native final-package reports from run 38693452893, on each actual target OS.
Each startup/resize is a single acceptance observation including routed input
and polling; engine measurements are warmed, exclude GUI/PTY and use the same
120×35 / 10k-history workload. Hardware/runner load differs; results are not ranked.

| Host | Startup to shell | Grid resize | Initial working set | Engine output | Full history snapshot/search | Engine reflow |
|---|---:|---:|---:|---:|---:|---:|

| osx-arm64 | 1517.0 ms | 100.0 ms | 191.6 MiB | 4.07 MiB/s | 152.2 ms | 66.2 ms |
| win-x64 | 1594.3 ms | 56.7 ms | 138.9 MiB | 3.26 MiB/s | 149.9 ms | 82.8 ms |
| linux-x64 | 2526.6 ms | 41.4 ms | 206.6 MiB | 3.91 MiB/s | 139.0 ms | 70.5 ms |

Raw JSON and scoped environments are in `evidence/v0.3.0-{rid}`. Native acceptance
includes replay operation, but this does not measure replay recorder CPU under
sustained output or guarantee a 16 MiB process working set. The replay byte
budget estimates retained frame objects; the frame-count bound is also enforced.

## Historical 0.2.0 local observation

Local environment: Apple M5, arm64, macOS 26.7.2; self-contained Release binary,
.NET 10.0.13/Avalonia 12.1.4. Raw JSON is in
[evidence/release-macos](evidence/release-macos). This is a reproducible acceptance
workload, not a comparison against iTerm2 or another terminal.

| Measurement | Local observation | What is measured |
|---|---:|---|
| Managed entry → real shell sentinel | 890.3 ms | One launch; includes native window, PTY, routed command and 25ms polling; excludes pre-Main process loading; not a cold-launch distribution |
| Native resize → changed grid | 26.3 ms | One resize, includes UI dispatcher/polling; PTY dimensions verified separately in integration tests |
| Initial working set | 198.7 MiB | One shell, native GUI; no forced GC |
| Engine output throughput | 3.66 MiB/s | 3MiB of UTF-8/wide/SGR text, 120x35, warmed engine, bounded 10k-line history |
| Output CPU time | 620.5 ms | Process CPU during the 819.8ms feed interval |
| Full history snapshot + search | 188.2 ms mean | 20 searches of retained history; includes snapshot allocation |
| Alternating engine resize | 69.8 ms mean | 100 reflows between 100x30 and 120x35 after large output |
| Working set after workload | 340.8 MiB | Process working set after searches/reflow; GC/allocator dependent |

PeakWorkingSet64 is unavailable (zero from macOS API) and stored as null, not
asserted to be zero memory. No comparative superiority, per-frame scroll
latency, long-duration CPU soak or memory-leak claim is made. Full-history
snapshot/search allocations and deep-history reflow are measurable costs;
this release bounds history rather than claiming constant-time operations.

Reproduce with `Aster.Terminal --benchmark report.json` from the extracted
package. `scripts/package.py --smoke` runs the same engine workload and native
acceptance on each target OS. Hosted Windows/Linux measurements are retained in
their release evidence; runner load and hardware differ, so cross-run timings
are not ranked. Linux uses Xvfb; hardware-GPU frame rates are not measured.
