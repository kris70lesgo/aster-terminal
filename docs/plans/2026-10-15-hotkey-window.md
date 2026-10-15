# System-wide summon window implementation plan

Goal: an opt-in Ctrl+Alt+Space (Control+Option+Space on macOS) binding summons a
separate real terminal window while Aster runs. Pressing it while that window is
active hides it without ending its shell. Closing the window ends its panes;
next summon creates a fresh window. Disabling the binding leaves an existing
window accessible. Settings must not silently replace the normal startup layout.

Architecture: use native registered hotkeys behind a Sessions platform interface:
Carbon RegisterEventHotKey on macOS, RegisterHotKey plus a dedicated Windows
message thread, checked XCB key grabs/event polling on X11. No raw keyboard hook,
keystroke recording, dependency bundle, agent install or VT-driven host action.
Wayland global shortcut portals need a separate implementation; report unsupported
rather than pretending XWayland receives compositor-wide input. Native failures
(collision/missing display) are visible; registrations/threads/handles dispose.

Alternatives considered: a third-party hotkey package, application-local shortcuts
only, or direct OS registration. Native registration avoids another runtime
package and provides explicit lifecycle/error behavior; local shortcuts cannot
meet the cross-application requirement. Start with the fixed portable space chord;
custom global gestures and upstream per-profile animated/dropdown styles are later
parity, not prerequisites for an actual summon window.

1. Add NativeGlobalHotkey platform registration/disposal. Study pinned iTerm2
   iTermCarbonHotKeyController.m; use SDK headers and primary Win32/XCB APIs.
   Keep callbacks narrow and dispatch UI actions through Avalonia.
2. Add a lazy hotkey-window manager, a settings checkbox and explicit show menu.
   Reuse MainWindow/real sessions but suppress normal layout restore/save in the
   dedicated window. Preserve variables across hide/show; closing still cleans up.
3. Test settings roundtrip and real-shell hide/show/close with injected key signals.
   Native fixtures must actually register/unregister each backend. Where permitted
   in CI, synthesize the global chord with another isolated Aster peer process
   foreground and verify the summoned window/retained shell, then cleanup.
4. Local Mac automation must preserve the user's foreground app. Do not inject
   global physical key events locally; use registration/native dispatch checks and
   optional human chord verification. Keep any missing physical-focus evidence
   explicit. No macOS permission dialog or security-setting change is automatic.
5. Run complete tests/native acceptance, update parity/licensing/migration evidence,
   audit release payload and recheck challenge rules before a later release.
