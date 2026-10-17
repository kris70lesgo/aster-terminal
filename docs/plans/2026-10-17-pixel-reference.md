# Pixel reference rendering pass

The target is the installed iTerm 3.7.4 window at 587 × 462 logical pixels,
Monaco 12, captured losslessly at 2×. Real user terminal captures remain private.
The screen text is live process output, never an image or simulated terminal.

Measured defects in 0.12.0:
- The comparison launcher inherited NO_COLOR=1 from the automation process.
  A fresh one-shot cua-driver launch without that automation-only variable restores
  fastfetch colours. Explicit app/profile NO_COLOR preferences must remain valid.
- iTerm's sRGB colour management maps cyan (96,253,255) to display-profile
  (142,250,253). Aster emits (96,253,255) directly. Do not bake this monitor's
  calibration into the portable palette.
- Bold ANSI blue remains index 4 rather than bright index 12 in Aster.
- Monaco 12 rows measure 16 logical pixels in Aster versus 17 in iTerm.
- get_window_state's screenshot path can contain JPEG data despite a .png suffix;
  pixel comparison must request screenshot format=png and inspect its signature.

Implementation:
1. Keep the pinned parser, engine, connection, settings and embedded fonts. Vendor
   only the pinned MIT control/render sources under Aster namespaces so rendering
   can be adjusted through normal source, without reflection or patching binaries.
   Record source hashes and retain attribution; no new external package versions.
2. Apply bold-to-bright only to foreground ANSI indexes 0–7 in the render planner.
   Preserve truecolour/indexed 8–255, backgrounds, inverse, and the VT snapshot.
3. Match Monaco's reference row geometry through the renderer's common cell
   measurement (drawing, hit testing, resize, replay and tmux all use it).
4. Use the macOS window colour space rather than monitor-specific RGB constants.
   Verify with a new installed-app lossless capture before keeping the change.
5. Compare native captures, run build/tests and native acceptance. Report measured
   remaining differences honestly; do not claim full pixel identity from a build.
