# Measured performance — 2026-10-16

## Background Find, local 0.8.0 observation

Apple M5 arm64, macOS 26.7.2, .NET 10.0.13, Release build. Ten warmed
iterations search 6,000 hard lines containing ASCII, CJK and decomposed Unicode
in a 120×35 engine with 10,000 history lines. Every run returns 6,000 matches
without a partial warning. Mean bounded capture / matching times:

| Mode | Capture | Match | Allocated bytes per operation |
|---|---:|---:|---:|
| Literal `target` | 23.2 ms | 31.7 ms | 50.8 MiB |
| Anchored Unicode regex | 29.8 ms | 21.6 ms | 50.1 MiB |

This isolates capture/matching without a GUI, PTY, host process or debounce.
Allocation is cumulative per-thread allocation, not retained memory or a process
working set. Matcher work runs off the UI thread; bounded capture holds the
engine gate, so snapshot allocation/lock time remains a measurable cost. These
observations establish neither full-cap worst-case latency nor a platform ranking.
The regex budget reports partial results for expensive patterns.

Raw per-iteration values and exact source are `evidence/find-benchmark-local.json`
and `evidence/find-benchmark-source.txt`. Reproduce in a Release net10.0 console
project referencing `src/Terminal.Sessions/Terminal.Sessions.csproj`; use the
source as Program.cs. No external benchmark package is required.

## Release 0.5.0

Combined native-package evidence is under `evidence/v0.5.0-{rid}`. Each startup/
resize observation includes real hosted-shell/input/polling; the UI process
working set excludes the separate shell host, PTY helper and shell, so it is
**not total application memory**. No comparative speed or memory claim is made.
The warmed engine benchmark still excludes GUI/PTY/hosting. Detailed process-tree
memory and recovery/long-soak performance remain unmeasured.

| Host | Startup to shell | Grid resize | UI working set only | Engine output | Full history snapshot/search | Engine reflow |
|---|---:|---:|---:|---:|---:|---:|
| osx-arm64 | 2241.9 ms | 82.2 ms | 200.8 MiB | 3.79 MiB/s | 151.2 ms | 75.5 ms |
| win-x64 | 1528.8 ms | 59.3 ms | 153.1 MiB | 4.64 MiB/s | 110.6 ms | 61.3 ms |
| linux-x64 | 3208.9 ms | 50.9 ms | 224.0 MiB | 3.27 MiB/s | 139.5 ms | 73.0 ms |

## Release 0.4.0

Final-package run 38700844528, each actual target OS. Single native startup/resize
observations include routed input/polling. Warmed engine workload is unchanged;
hardware and runner load differ. No ranking or comparative speed claim is made.

| Host | Startup to shell | Grid resize | Initial working set | Engine output | Full history snapshot/search | Engine reflow |
|---|---:|---:|---:|---:|---:|---:|
| osx-arm64 | 974.5 ms | 98.9 ms | 202.8 MiB | 4.72 MiB/s | 110.0 ms | 55.2 ms |
| win-x64 | 1174.6 ms | 42.7 ms | 144.1 MiB | 4.59 MiB/s | 105.0 ms | 60.0 ms |
| linux-x64 | 2575.1 ms | 30.9 ms | 210.7 MiB | 3.93 MiB/s | 183.7 ms | 62.8 ms |

The engine benchmark runs without profile rules. Native acceptance uses one rule
and verifies graphics/replay correctness, not sustained rule/graphics throughput.
Ordinary trigger lines copy one row; wrapped triggers map retained history and
have additional snapshot allocation cost. Burst matching can skip lines under
its documented work budget; no lossless monitoring or long-soak claim is made.

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
