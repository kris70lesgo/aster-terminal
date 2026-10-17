# Avalonia Port Challenge compliance

Verified 2026-10-14; rechecked 2026-10-17 (Asia/Kolkata) for the final 0.10.0 milestone. This is a completed project checklist with explicit remaining entrant/form items, not a claim of submission.
Authority for every contest rule below: [official announcement, published 2026-08-28](https://avaloniaui.net/blog/avalonia-port-challenge).

| Rule / requirement | Verification date | Status | Repository evidence / remaining work |
|---|---|---|---|
| Original application predates contest opening; ports and rewrites allowed | 2026-10-14 | VERIFIED, candidate eligible | iTerm2 predates 2026; pinned source inventory in `iterm2-feature-parity.md`. This is a rewrite of a subset, not a claim of full parity. |
| Someone else's application may be ported if its licence permits | 2026-10-14 | VERIFIED; audit complete for reused subset | `licensing.md`: upstream COPY­ING grants GPLv2-or-later, full application effectively GPLv3. Aster uses GPL-3.0-or-later. |
| Copyleft port cannot be closed source | 2026-10-14 | VERIFIED; compliant public GPL source | GPL source and notices in this repository; no closed-source distribution planned. |
| Open-source entry: public GitHub repository with OSI-approved licence | 2026-10-14 | COMPLETE | Public https://github.com/kris70lesgo/aster-terminal; LICENSE, source, adapted scripts/helper and build scripts. Dependency notices/source map reviewed. |
| Downloadable builds for each targeted platform | 2026-10-14 | COMPLETE | All native final-binary checks pass; self-contained macOS arm64/Windows x64/Linux x64 archives and SHA-256 are published at https://github.com/kris70lesgo/aster-terminal/releases/tag/v0.10.0. test-matrix.md and ci-v0.10.0.json record evidence. |
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

2026-10-15 0.3.0 milestone recheck: the official announcement remains unchanged. Public GPL
source and final self-contained package checks pass on all three OSes in run
38693452893. Versioned evidence includes minimal UI, settings and text replay.
The update retains explicit advanced-parity/manual-test gaps. Entrant identity,
form-only declarations and actual submission remain separate.

2026-10-15 0.4.0 milestone recheck: the same official announcement was read
again and requirements are unchanged. Source 1cf4a149 / run 38700844528 adds
safe output rules, verified inline graphics and anchored scrollback. All three
native packages and the installed Retina Mac pass. SHA-256, executable modes,
macOS extracted/installed signatures and complete notices were audited before
publication. GPL corresponding source and evidence accompany the downloads;
entrant declarations and submission remain unverified/unperformed.

2026-10-15 0.5.0 milestone recheck: announcement reread through curl after a
web-reader timeout, with unchanged requirements. Final source 96a90b4 / run
38752301646 passes all three native packages including crash/reopen; Unix
packages additionally pass actual native tmux control attach. Current-user
recovery and explicit local control integration add no dependency/bundled tmux
source. Release payloads retain GPL, runtime, dependency and font notices.
Entrant declarations and actual form submission remain separate.

2026-10-16 0.6.0 milestone recheck: official announcement reread, same deadline
and public GPL/build/screenshot/write-up requirements. Source c651f5b / native
run 38763544008 passes all three platforms, including global summon and recovery.
Exact archives/notices/checksums and installed Retina Mac verified. No new bundled
dependency. Form submission and entrant declarations remain separate.

2026-10-16 0.7.0 milestone recheck: same official deadline, public GPL,
platform-download, screenshot and migration-write-up requirements. Final source
ef6d642 / run 38792785139 passes all native targets including the original Python
client core. New protobuf/ASP.NET/runtime and build-only tool licences/source
provenance audited; exact release archives retain notices and source references.
Installed Retina Mac passes. Entrant declarations and actual submission remain
separate; no form submission was performed.

2026-10-16 0.8.0 milestone recheck: official announcement retrieved through curl
following a web-reader timeout. Requirements and deadline are unchanged; retrieval
hash/date are in evidence/rules-v0.8.0.json. Source 4d4f5b3 / run 38851975830
passes final native checks on all three targets, with exact installed Retina Mac
acceptance. Search adaptation preserves the pinned MIT attribution and full
existing notices; no new dependency. Checksums, signatures and payload inventory
are audited before publication. Entrant identity/form declarations remain
unverified and no form submission is claimed.

2026-10-16 0.8.1 milestone recheck: the official announcement was retrieved
through the web reader. Deadline and public source/build/screenshot/write-up
requirements remain unchanged. The small new Mac shutdown bridge is Aster GPL
source using system AppKit/dispatch APIs; source and build target are public.
Final native run 38901660170 / source d23eba2 passes Windows, macOS and Linux.
Package/notices/deployment-target and exact installed Retina checks are audited
before publishing. Entrant identity/form declarations remain unverified and no
submission is claimed.

2026-10-16 0.9.0 milestone recheck: the unchanged official announcement was
retrieved through curl after web-reader timeouts. Deadline and public GPL source,
target builds, screenshots and README write-up requirements are unchanged;
retrieval provenance is in evidence/rules-v0.9.0.json. Shared tmux Find adds no
dependency or protocol action. Final native run 38906845116 passes all three targets (78 tests including
platform skips, zero failures). Payload checksums, required notices, executable
modes, Mac deployment target/system links/signatures and exact installed bytes
are audited in evidence/payload-v0.9.0.json. The earlier zero-display failure remains retained. After display wake, the
unchanged exact installed Mac archive passes the full 2× Retina batch with three
scripting/native-loop exits in evidence/v0.9.0-macos-retina. Entrant identity/form declarations remain
unverified and no submission was performed.

2026-10-17 0.10.0 milestone: official announcement rechecked through the web
reader; requirements/deadline unchanged, provenance in rules-v0.10.0.json.
A separate raw-page fetch returned HTTP 403, so no new raw HTML hash is claimed.
Live tab ordering adds no dependency or licensing change. Final source 87184b0
/ run 39001628564 passes native acceptance on all three targets, and exact
installed Retina acceptance passes all eight fixture exits. Archives/notices/
signatures/installed bytes audited in payload-v0.10.0.json. Entrant declarations
and the entry form remain unverified; no contest submission has been made.
