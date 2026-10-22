# Avalonia Port Challenge compliance

Verified 2026-10-22 (UTC) for the public 0.14.0 release. This project checklist covers public materials and clearly separates the entrant-only form declarations; it does not claim the entry has been submitted. Authority for the current rules is the [official announcement](https://avaloniaui.net/blog/avalonia-port-challenge), rechecked today.

| Rule / requirement | Verification date | Status | Repository evidence / remaining work |
|---|---|---|---|
| Original application predates contest opening; ports and rewrites allowed | 2026-10-22 | VERIFIED, candidate eligible | iTerm2 predates 2026; pinned source inventory in `iterm2-feature-parity.md`. Aster is an independent rewrite of a documented subset, not full parity. |
| Someone else's application may be ported if its licence permits | 2026-10-22 | VERIFIED for reused scope | `licensing.md`: upstream COPYING grants GPLv2-or-later; Aster is GPL-3.0-or-later. Reused components and notices are listed and preserved. |
| Copyleft port cannot be closed source | 2026-10-22 | VERIFIED | Public source and release payload are GPL; required project/dependency notices ship in every archive. |
| Public repository under an OSI-approved licence | 2026-10-22 | COMPLETE | [Public GitHub repository](https://github.com/kris70lesgo/aster-terminal), GPL-3.0-or-later source, adapted scripts/helpers and build files. |
| Downloadable builds for each target platform | 2026-10-22 | COMPLETE | Public [v0.14.0 release](https://github.com/kris70lesgo/aster-terminal/releases/tag/v0.14.0) has macOS arm64, Windows x64 and Linux x64 archives plus SHA-256 sidecars. All match `payload-v0.14.0.json`; final native run is `ci-v0.14.0.json`. |
| Before-and-after screenshots | 2026-10-22 | COMPLETE with limits | README links the original iTerm2 startup reference and native Aster captures. Different app/reference versions and output are identified; whole-window pixel identity is not claimed. |
| Short migration write-up in README, including actual costs | 2026-10-22 | COMPLETE; direct cost unmeasured | README describes architecture, problems, eight calendar days of delivery and states that hands-on hours, AI spend and cash outlay were not tracked. No numerical cost is fabricated. |
| Five judging dimensions: migration difficulty/completeness, application quality, cross-platform implementation, engineering quality, usefulness of write-up | 2026-10-22 | Evidence mapped | `migration-report.md`, `test-matrix.md`, `iterm2-feature-parity.md` and README. |
| Best Cross-Platform Port: one codebase shipping on Windows, macOS and Linux | 2026-10-22 | Three-platform subset verified | Avalonia host/platform adapters and final self-contained package acceptance on all three OSes in CI run 39748978754. Limits remain explicit in `test-matrix.md`. |
| Best IT Pro Tool | 2026-10-22 | Plausible category candidate | A terminal is an interactive IT tool; category selection and judging remain the entrant's choice. |
| Solo entry or team of up to three; one prize per team | 2026-10-22 | Rules verified; entrant declaration remains | Human entrant/team details and eligibility declarations belong in the form. AI assistance is allowed. |
| Deadline 23 October 2026, 23:59 UTC; winners 6 November | 2026-10-22 | VERIFIED from official page | Deadline is 24 October 2026, 05:29 IST. Do not assume an extension. |
| Submit using the linked entry form | 2026-10-22 | Entrant action remains | User asked that the form not be submitted. The entrant must complete personal and legal declarations truthfully. No submission was made. |
| Mobile/WebAssembly receive additional recognition | 2026-10-22 | Verified; not implemented | Current project targets desktop terminal workflows. |
| Required minimum terminal feature list | 2026-10-22 | No prescriptive list found | Judges assess migration and resulting quality; project parity scope is documented separately. |

Recheck the official announcement immediately before the entrant submits. Form-specific declaration wording and the entrant's eligibility are not verified here. The unused iTerm2 submodules are not claimed as audited or imported. The project is not represented as complete iTerm2 feature/UI parity.


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

2026-10-17 0.11.0 UI milestone: official announcement checked during this session;
requirements/deadline unchanged, provenance in rules-v0.11.0.json. No dependency
or reused asset change. All three final native packages pass run 39003323494;
exact installed Retina and full payload/notice checks pass as documented in
ci-v0.11.0.json and payload-v0.11.0.json. Visual comparisons remain scoped in
visual-parity.md. Entrant identity/form declarations remain unverified and no
contest form was submitted.

2026-10-17 0.12.0 installed-reference milestone: official announcement rechecked;
requirements and 23 October 23:59 UTC deadline unchanged (rules-v0.12.0.json).
Native final source e4e1646 / run 39009937189, installed Retina and all payload/
notice checks pass. Private user shell captures are not published; isolated
real-shell fixture captures document the UI. No new dependency/upstream asset.
Entrant identity/form declarations remain unverified; no form submitted.

Historical 2026-10-19 preflight (superseded by the 2026-10-22 table above):
the announcement then required public source/licence, builds for each target,
before-and-after screenshots, and an open-source README migration write-up. At
that point v0.13.0 was latest; its artifacts and checksum audit remain historical
evidence. The entry form was inspected without submission; personal identity,
eligibility and legal confirmations remain for the entrant. UI work subsequently
entered v0.14.0 as documented above.


2026-10-22 final release verification: v0.14.0 is published at
https://github.com/kris70lesgo/aster-terminal/releases/tag/v0.14.0. GitHub exposes all
six expected assets (three packages and three SHA-256 files); each archive digest
reported by GitHub matches the local CI artifact and each downloaded sidecar
contains that archive's matching hash. The entry form remains unsubmitted at the
user's direction. Only entrant identity and truthful eligibility/legal responses
remain before the user submits.
