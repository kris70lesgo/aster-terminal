# Avalonia Port Challenge — entry preparation

This is a submission pack for the entrant to review and submit. The entry form
has not been filled or sent.

## Entry links

- Project: **Aster Terminal**, a GPL-3.0-or-later Avalonia rewrite of a documented iTerm2 subset.
- Public repository: <https://github.com/kris70lesgo/aster-terminal>
- Release: <https://github.com/kris70lesgo/aster-terminal/releases/tag/v0.14.0>
- macOS arm64: <https://github.com/kris70lesgo/aster-terminal/releases/download/v0.14.0/aster-terminal-0.14.0-osx-arm64.zip>
- Windows x64: <https://github.com/kris70lesgo/aster-terminal/releases/download/v0.14.0/aster-terminal-0.14.0-win-x64.zip>
- Linux x64: <https://github.com/kris70lesgo/aster-terminal/releases/download/v0.14.0/aster-terminal-0.14.0-linux-x64.tar.gz>
- Migration write-up: [README migration section](../README.md#migration-write-up) · [Detailed migration story](port-challenge-story.md)
- Before screenshot: [iTerm2 reference](evidence/iterm2-original.png) · [direct GitHub image](https://github.com/kris70lesgo/aster-terminal/blob/main/docs/evidence/iterm2-original.png)
- After screenshot: [Aster native shell window](evidence/v0.14.0-osx-arm64/dark.png) · [direct GitHub image](https://github.com/kris70lesgo/aster-terminal/blob/main/docs/evidence/v0.14.0-osx-arm64/dark.png)
- Three-platform test/evidence record: [verification matrix](test-matrix.md), [CI JSON](evidence/ci-v0.14.0.json), [payload audit](evidence/payload-v0.14.0.json)
- Contest announcement and entry form: <https://avaloniaui.net/blog/avalonia-port-challenge> (the form is linked near the end of the official page; its current destination is <https://docs.google.com/forms/d/e/1FAIpQLSfJsgz1HRHKsxH0yRnn2wcvbZ-tSkmnjDXxIhE5YjM1v3lVkg/viewform?usp=send_form>).

## Entrant-only fields

Complete these personally in the official form and check every eligibility,
licensing, privacy and terms statement against your own situation:

- Entrant or team name: ____________________
- Contact email: ____________________
- Country/residence and any age-bracket field: ____________________
- GitHub username: ____________________ (repository owner is `kris70lesgo`; confirm this is the entrant's account)
- Team size and eligibility: ____________________
- Required original-work, third-party-license, and terms/privacy confirmations: review and answer truthfully in the form.

Do not copy the repository owner into the entrant field without confirming it.
The project is a port of iTerm2, not an original iTerm2 implementation; its
GPL-compatible licensing and attribution are documented in the repository.

## Claims to keep accurate

The 0.14.0 packages passed the final three-platform test and native package run
on 22 October 2026. The port implements a substantial terminal subset; it does
not implement full iTerm2 UI or feature parity. The screenshots show real
applications and controlled shell output, but are not pixel-identical matched
screenshots. The development write-up gives an eight-calendar-day delivery span
and explicitly says human hours, AI spend and total cash cost were not tracked.
macOS is ad-hoc signed without Developer ID notarization; Windows is unsigned;
Linux Wayland support remains unverified.

The contest closes at **23:59 UTC on 23 October 2026 (05:29 IST on 24 October)**
according to the [official announcement](https://avaloniaui.net/blog/avalonia-port-challenge).
