# Avalonia Port Challenge compliance

Verified 2026-10-14 (Asia/Kolkata). This is an initialization audit, not a completed submission.
Authority for every contest rule below: [official announcement, published 2026-08-28](https://avaloniaui.net/blog/avalonia-port-challenge).

| Rule / requirement | Verification date | Status | Repository evidence / remaining work |
|---|---|---|---|
| Original application predates contest opening; ports and rewrites allowed | 2026-10-14 | VERIFIED, candidate eligible | iTerm2 predates 2026; pinned source inventory in `iterm2-feature-parity.md`. This is a rewrite of a subset, not a claim of full parity. |
| Someone else's application may be ported if its licence permits | 2026-10-14 | VERIFIED; release audit pending | `licensing.md`: upstream COPY­ING grants GPLv2-or-later, full application effectively GPLv3. Aster uses GPL-3.0-or-later. |
| Copyleft port cannot be closed source | 2026-10-14 | VERIFIED; planned compliant | GPL source and notices in this repository; no closed-source distribution planned. |
| Open-source entry: public GitHub repository with OSI-approved licence | 2026-10-14 | PENDING | Local repository only; public remote and complete corresponding source not yet published. |
| Downloadable builds for each targeted platform | 2026-10-14 | PENDING | Windows, macOS, Linux targeted; runtime evidence needed on each OS. Local build is not a public download. |
| Before-and-after screenshots | 2026-10-14 | PENDING | `docs/evidence/`; equivalent original and port states with attribution required. |
| Mandatory short migration write-up covering actual costs, in README for open source | 2026-10-14 | IN PROGRESS | `migration-log.md`, `migration-report.md`, README; no invented time or expenses. |
| Five judging dimensions: migration difficulty/completeness, application quality, cross-platform implementation, engineering quality, usefulness of write-up | 2026-10-14 | VERIFIED; evidence incomplete | `migration-report.md`, `test-matrix.md`, feature matrix. |
| Best Cross-Platform Port: one codebase shipping cleanly on Windows, macOS, Linux | 2026-10-14 | VERIFIED; target NOT achieved | Shared Avalonia host plus platform adapters; unavailable OS runtime testing remains pending. |
| Best IT Pro Tool: sysadmin/security/networking/remote access tooling | 2026-10-14 | VERIFIED; secondary candidate | Interactive terminal scope fits category description; category acceptance is judges' decision. |
| Solo entry or team up to three; one prize per team | 2026-10-14 | VERIFIED; entrant details UNVERIFIED | Human entrant/team declaration needed at submission. AI assistance explicitly allowed. |
| Deadline 23 October 2026, 23:59 UTC; winners 6 November | 2026-10-14 | VERIFIED | Deadline is 24 October 2026, 05:29 India time. Do not assume extension. |
| Submit via linked entry form | 2026-10-14 | VERIFIED link; form fields UNVERIFIED | Announcement links Google form; web reader could not retrieve form. No submission performed. |
| Mobile/WebAssembly receive additional recognition | 2026-10-14 | VERIFIED; out of current scope | Desktop terminal takes priority. |
| Required minimum terminal feature list | 2026-10-14 | No prescribed list found | Announcement evaluates completeness and quality; Phase 1/2 acceptance criteria are project goals, not invented contest rules. |

Reverify at architectural eligibility changes, major milestones, release-artifact preparation, and immediately before submission. Retain this dated audit; do not refetch unchanged rules for routine edits.

Ambiguities: form-specific declarations/terms are UNVERIFIED; no official guarantee of acceptance for this partial rewrite; full upstream dependencies are not yet individually audited for copying. Publishing a public repository or submitting the entry is separate from local engineering.
