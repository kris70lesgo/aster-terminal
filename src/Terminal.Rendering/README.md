# Pinned native terminal presentation

This project adapts the control and Skia rendering sources from
[Devolutions Terminal](https://github.com/Devolutions/devolutions-terminal/tree/fa23083f1bda1a60fe2b322c6cfab4a616af9edb),
commit `fa23083f1bda1a60fe2b322c6cfab4a616af9edb`, under the retained
[MIT notice](../../licenses/Devolutions-MIT.txt). `upstream.json` records the
original paths and SHA-256 hashes before adaptations. The original parser,
engine, settings, connections, and embedded fonts still come from the locked
2026.3.4 package. Its original Control/Render assemblies remain in the licensed
package payload but are not the application's presentation implementation.
No system font is copied or redistributed.

Local changes, beyond the `Aster.Terminal` / `Aster.Terminal.Render` namespaces:

- `TerminalRenderPlanner`: resolve bold low ANSI foregrounds through the bright
  palette, leaving snapshots, backgrounds, extended indices and RGB untouched.
- `SkiaTerminalRenderer`: round cell ascent/descent/leading outwards, use native
  Mac hinting, and synthesize missing Mac bold faces by a second strike instead
  of expanding outlines; use grayscale antialiasing and a 0.2 synthetic italic shear. This is a new C# implementation informed by iTerm2's
  FontSizeEstimator.m / PTYTextView.m / iTermTextDrawingHelper.m at the project's
  pinned iTerm commit; GPL-3.0-or-later adaptations, George Nachman/contributors.
- `FontResolver`: prefer native/bundled colour emoji for default emoji
  presentation or VS16, preserving VS15 text presentation. The presentation
  predicate is adapted from the same pinned Core/WcWidth.cs; widths stay in Core.
- Renderer/draw-operation contracts: respect separate vertical/horizontal
  padding for painting, matching the control's layout, hit testing and IME.
  Keep cursor/selection/images/search on the same coordinate system.

The extra presentation assembly is deliberate: upstream's control is sealed
and constructs its renderer privately. A normal source adaptation makes these
changes reviewable without reflection, binary patching, or terminal engine
changes. When upgrading the pin, compare every source file with its recorded
upstream version, reapply the listed changes, and run the full test/native
acceptance matrix (including graphics, Find, replay, scrollbar and tmux grids).
