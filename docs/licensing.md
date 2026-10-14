# Licensing audit — 2026-10-14

Pinned iTerm2: `069b6e0dd8f3f0d82fc08ea5d294d28f7c5b986e` ([source](https://github.com/gnachman/iTerm2/tree/069b6e0dd8f3f0d82fc08ea5d294d28f7c5b986e)). Reviewed README, LICENSE, COPYING, README.license, ThirdParty/Licenses.txt, .gitmodules, selected terminal/session sources and headers.

README says GPLv3, LICENSE and README.license contain GPLv2. [COPYING](https://github.com/gnachman/iTerm2/blob/069b6e0dd8f3f0d82fc08ea5d294d28f7c5b986e/COPYING) explicitly says GPLv2-or-later and explains the full application is effectively GPLv3 because of Apache-2.0 dependencies. This supports electing GPLv3 for adapted covered components; the README and generic licence text alone do not settle the grant. Some source files have their own permissive notices, which must remain intact if copied. Do not assume every file has the same licence.

Aster-authored and iTerm2-adapted code is GPL-3.0-or-later. Preserve original attribution, identify adaptations and dates, include licence text, and distribute complete corresponding source/build scripts with binary releases. Do not imply official iTerm2 endorsement. No public release authorised by this local audit alone; an artifact-specific dependency/notice review is still required.

## Reuse ledger

| Material | Basis | Treatment |
|---|---|---|
| iTerm2 OSC 133 exit-code scan and OSC 1337 metadata behavior | COPY­ING GPLv2-or-later, elect GPLv3; VT100Terminal.m `exitCodeFromArgs`, `executeXtermSetKvp` | C# adaptation, upstream attribution in source and NOTICE; generic LICENSE file is not copied as our GPLv3 licence |
| Devolutions.Terminal.Control 2026.3.4 + bundled Core/Render/Connection/Settings | MIT, nuspec/source at `fa23083f1bda1a60fe2b322c6cfab4a616af9edb` | Retain MIT notice; only terminal APIs used, no cloud/ISE features |
| `dt-pty-host.c` from same pinned source | Project MIT; Unix forkpty helper | Vendor source and MIT licence, retain notices, record local changes |
| Avalonia 12.1.4 | MIT | Preserve framework notices with distribution |
| SkiaSharp/HarfBuzzSharp transitives | Package/native dependency licence trees | Release inventory pending, include actual runtime notices before distribution |
| Bundled Cascadia Mono, Noto Color Emoji in control | SIL OFL-1.1 per upstream NOTICE | Include full font licences/notices even though fonts are embedded in the control |
| Microsoft.Windows.Console.ConPTY | Package-specific notices shipped by package | Preserve third-party notices and audit actual Windows payload before release |
| .NET runtime if self-contained | MIT plus third-party notices | Include runtime notices in release payload |
| xUnit and test SDK | Test-only dependencies | Not in application runtime payload; package licence inventory still recorded |

Restored-package inventory: `scripts/dependency-licenses.py` records all resolved versions from lockfiles. Runtime package metadata is MIT except ANGLE's licence file, inspected as BSD-3-Clause and retained in `licenses/ANGLE-BSD.txt`. SkiaSharp and HarfBuzzSharp native package licence and third-party notices are retained under `licenses/`. Their broad notice collection includes BSD/MIT/Apache material, MPL/GPL/LGPL tri-licensed components, FreeType licence alternatives and LGPL/eCos material. It is not proof that every listed component is compiled into our payload. **Exact native-component/selected-licence verification remains unresolved before public distribution**; do not infer compatibility of all native contents from the NuGet MIT label. Embedded fonts remain OFL-1.1. Corresponding source access and final artifact notices remain release gates.

iTerm2 submodules include NMSSH, CoreParse, Sparkle, libsixel, OpenSSL, libssh2, FMDB, libgit2, shell integration, MultiCursor, SwiftyMarkdown, Highlightr, Powerline symbols, BTree, railroad_dsl, adblock-rust, SFSymbolEnum, noise-c and companion relay. These are **not imported**. Their gitlink SHAs can be obtained from the pinned tree. ThirdParty/Licenses.txt documents MIT/BSD and other material; it is evidence, not proof that all submodules are audited. Full per-submodule audit is pending before any further reuse. Current adaptation imports no upstream binary, assets, shell scripts or vendored libraries.
