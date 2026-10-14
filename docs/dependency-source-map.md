# Corresponding dependency sources and release notices

Aster source, build scripts, pinned helper and adapted shell scripts are public in
https://github.com/kris70lesgo/aster-terminal. No source offer is substituted for
available source. Package lockfiles preserve exact restored versions/hashes.

| Distributed material | Original source / version | Licence and notice treatment |
|---|---|---|
| iTerm2 OSC algorithms | gnachman/iTerm2 `069b6e0dd8f3f0d82fc08ea5d294d28f7c5b986e`, VT100Terminal.m | GPLv2-or-later, elect GPLv3; adaptation attribution in source and NOTICE |
| iTerm2 bash/zsh integration | gnachman/iTerm2-shell-integration `bcd52317a70f1a489b86fcfd1ee97df155741092` | GPLv2-or-later, elect GPLv3; original headers retained, zsh gate change marked; included bash-preexec MIT notice retained |
| Devolutions control and internal Core/Render/Connection/Settings | https://github.com/Devolutions/devolutions-terminal/tree/fa23083f1bda1a60fe2b322c6cfab4a616af9edb | MIT; full notice retained. Native helper modified for Linux session cleanup and included as source |
| Avalonia 12.1.4 (all UI backends) | https://github.com/AvaloniaUI/Avalonia/tree/12.1.4 | MIT; `licenses/Avalonia-MIT.txt` |
| SkiaSharp/HarfBuzzSharp 3.119.4 / 8.3.1.5 | https://github.com/mono/SkiaSharp/tree/f568ac94dd768ef9a2f593537cfde2dd0d348ef5 | MIT wrappers, exact native-package notices retained |
| Skia native dependency tree | https://github.com/mono/skia/tree/7dbfc07dd33181f84e0958afb7ee805c6c769f0b | BSD Skia; `DEPS` pins externals; SkiaSharp `native/*/build.cake` and `scripts/cake/native-shared.cake` are reproducible native build definitions |
| MicroCom.Runtime 0.11.6 | https://github.com/kekekeks/MicroCom/tree/76785efcafd91b5902fd19dd11145f6dd655b7b4 | MIT copyright Nikita Tsukanov; full notice retained |
| Tmds.DBus.Protocol 0.94.1 | https://github.com/tmds/Tmds.DBus/tree/b4a7fed0b878f74cb54f7cca84d2889af4e596ba | MIT copyright Tom Deseyn; full notice retained |
| Microsoft ConPTY 1.24.260710001 | Package/source reference in lockfile, https://github.com/microsoft/terminal | MIT; original shipped third-party notices retained |
| .NET runtime 10.0.13 | https://github.com/dotnet/runtime/tree/v10.0.13 | Runtime package LICENSE and THIRD-PARTY-NOTICES retained in every package |
| Cascadia Mono / Noto Color Emoji embedded in control | Microsoft Cascadia Code / Google Noto Emoji, pinned by Devolutions source tree | OFL-1.1, unmodified fonts; full notices retained |
| ANGLE native assets | Package 2.1.27548.20260419, native source referenced by Avalonia | BSD-3-Clause; full package licence retained |

## Native notice collection audit

Reviewed SkiaSharp's pinned macOS/Linux/Windows GN build targets: Skia and skottie
are enabled; ICU and embedded HarfBuzz inside Skia are disabled; separate
HarfBuzzSharp builds the pinned HarfBuzz external. Image libraries include PNG,
JPEG, WebP, zlib, Expat and piex (plus platform-specific DNG/FreeType). Native
package notices contain their permissive grants and are shipped intact.

The broad notices also list old optional microhttpd and tri-licensed Mozilla
material. microhttpd is absent from the pinned Skia DEPS/native build targets;
it is not a new application dependency. For tri-licensed material the
GPLv2-or-later option is compatible with elected GPLv3; FreeType's FTL alternative
is selected where included. All original attributions remain in the bundle,
including unused notice entries. Do not infer additional application functionality
from a dependency's optional code or broad notice inventory.

macOS packaging uses ad-hoc signing, not Developer ID notarization. Windows
archives are unsigned. These distribution properties are disclosed in installation
instructions. System libraries (macOS frameworks, Linux desktop/font libraries,
Windows console components) are supplied by the host, not secretly relicensed.
