# Avalonia Port Challenge compliance

Verified 2026-10-14 (Asia/Kolkata). Rechecked at release preparation. This is a completed project checklist with explicit remaining entrant/form items, not a claim of submission.
Authority for every contest rule below: [official announcement, published 2026-08-28](https://avaloniaui.net/blog/avalonia-port-challenge).

| Rule / requirement | Verification date | Status | Repository evidence / remaining work |
|---|---|---|---|
| Original application predates contest opening; ports and rewrites allowed | 2026-10-14 | VERIFIED, candidate eligible | iTerm2 predates 2026; pinned source inventory in `iterm2-feature-parity.md`. This is a rewrite of a subset, not a claim of full parity. |
| Someone else's application may be ported if its licence permits | 2026-10-14 | VERIFIED; audit complete for reused subset | `licensing.md`: upstream COPY­ING grants GPLv2-or-later, full application effectively GPLv3. Aster uses GPL-3.0-or-later. |
| Copyleft port cannot be closed source | 2026-10-14 | VERIFIED; compliant public GPL source | GPL source and notices in this repository; no closed-source distribution planned. |
| Open-source entry: public GitHub repository with OSI-approved licence | 2026-10-14 | COMPLETE | Public https://github.com/kris70lesgo/aster-terminal; LICENSE, source, adapted scripts/helper and build scripts. Dependency notices/source map reviewed. |
| Downloadable builds for each targeted platform | 2026-10-14 | COMPLETE | All native final-binary checks pass; self-contained macOS arm64/Windows x64/Linux x64 archives and SHA-256 are published at https://github.com/kris70lesgo/aster-terminal/releases/tag/v0.2.0. test-matrix.md and ci-release.json record evidence. |
| Before-and-after screenshots | 2026-10-14 | COMPLETE with limits | Original iTerm2 3.6.6 and native Aster initial/tabs/splits/search/preferences/dark/light captures under docs/evidence. Different version/profile/geometry explicitly disclosed; no pixel identity asserted. |
| Mandatory short migration write-up covering actual costs, in README for open source | 2026-10-14 | COMPLETE | README contains migration write-up, decisions/problems and unmeasured cost declaration; migration-log/report/performance provide supporting detail. |
| Five judging dimensions: migration difficulty/completeness, application quality, cross-platform implementation, engineering quality, usefulness of write-up | 2026-10-14 | VERIFIED; evidence mapped | `migration-report.md`, `test-matrix.md`, feature matrix. |
| Best Cross-Platform Port: one codebase shipping cleanly on Windows, macOS, Linux | 2026-10-14 | VERIFIED; three-OS subset demonstrated | Shared Avalonia host/platform adapters; hosted real-process tests and native self-contained package acceptance on macOS/Windows/Linux. Scope and manual gaps in test-matrix.md. |
| Best IT Pro Tool: sysadmin/security/networking/remote access tooling | 2026-10-14 | VERIFIED; secondary candidate | Interactive terminal scope fits category description; category acceptance is judges' decision. |
| Solo entry or team up to three; one prize per team | 2026-10-14 | VERIFIED; entrant details UNVERIFIED | Human entrant/team declaration needed at submission. AI assistance explicitly allowed. |
| Deadline 23 October 2026, 23:59 UTC; winners 6 November | 2026-10-14 | VERIFIED | Deadline is 24 October 2026, 05:29 India time. Do not assume extension. |
| Submit via linked entry form | 2026-10-14 | VERIFIED link; form fields UNVERIFIED | Announcement links https://forms.gle/bgvQLAmJpTCNcqwR9; release-time recheck still failed to retrieve form through web reader. Form-specific terms/declarations UNVERIFIED. No submission performed. |
| Mobile/WebAssembly receive additional recognition | 2026-10-14 | VERIFIED; out of current scope | Desktop terminal takes priority. |
| Required minimum terminal feature list | 2026-10-14 | No prescribed list found | Announcement evaluates completeness and quality; Phase 1/2 acceptance criteria are project goals, not invented contest rules. |

Reverify at architectural eligibility changes, major milestones, release-artifact preparation, and immediately before submission. Retain this dated audit; do not refetch unchanged rules for routine edits.

Ambiguities: form-specific declarations/terms are UNVERIFIED; no official guarantee of acceptance for a subset rewrite. Unused iTerm2 submodules are not imported or asserted fully audited; current reused components have their own source/notice audit. Public source is published. Entrant/team declaration and form submission remain separate from completing the app; recheck rules immediately before submission.

0.3.0 preparation recheck: the official announcement remains unchanged. New
UI/replay source and local tests are complete; final native builds/evidence and
publishing are pending. The published 0.2.0 remains downloadable meanwhile.
