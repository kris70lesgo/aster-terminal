"""Real upstream-client acceptance, run only against an owned Aster fixture."""
import asyncio
import importlib.metadata
import json
import sys
from pathlib import Path
import iterm2

checks = []


async def until(predicate, seconds=15):
    async def wait():
        while not await predicate():
            await asyncio.sleep(0.05)
    await asyncio.wait_for(wait(), seconds)


async def main(connection):
    app = await iterm2.async_get_app(connection)
    window = app.current_terminal_window
    assert window is not None and len(window.tabs) == 1
    original = window.current_tab.current_session
    original_id = original.session_id
    checks.append("Original iterm2 2.30 async_get_app lists the real window/tab/session and subscribes to layout/focus notifications")
    extra = await iterm2.Window.async_create(connection)
    assert extra is not None and extra.window_id != window.window_id
    await app.async_refresh()
    assert len(app.windows) == 2
    extra_session = extra.current_tab.current_session.session_id
    await extra.current_tab.async_close(force=True)
    await app.async_refresh()
    assert app.get_session_by_id(extra_session) is None and len(app.windows) == 1
    checks.append("Original client creates a separate native window and closes its only tab without ending the original window")
    tab = await window.async_create_tab()
    session = tab.current_session
    pane = await session.async_split_pane(vertical=True, before=True)
    assert pane.session_id != session.session_id != original_id
    checks.append("Original client creates a real tab and split pane with distinct stable session identities")
    # Use the fixture's platform, not the developer's ordinary shell startup.
    if sys.platform == "win32":
        await pane.async_send_text("set ASTER_SCRIPT_VALUE=live\r")
        command = "echo __PYTHON_API__%ASTER_SCRIPT_VALUE% 東京 é\r"
    else:
        await pane.async_send_text("ASTER_SCRIPT_VALUE=live\n")
        command = "printf '\\n__PYTHON_API__%s 東京 é\\n' \"$ASTER_SCRIPT_VALUE\"\n"
    await pane.async_send_text(command)
    async def has_value():
        screen = await pane.async_get_screen_contents()
        return any("__PYTHON_API__live 東京 é" in screen.line(i).string for i in range(screen.number_of_lines))
    await until(has_value)
    await asyncio.sleep(0.4)  # Native fixture captures the actual rendered pane before close.
    checks.append("Original send_text and get_screen_contents preserve a shell-local variable, wide Unicode and a combining grapheme")
    await app.async_refresh()
    refreshed = app.get_session_by_id(pane.session_id)
    assert refreshed is not None and refreshed.grid_size.width > 0
    assert len(app.get_window_by_id(window.window_id).tabs) == 2
    checks.append("Updated layout notifications and explicit refresh describe actual tab/pane grids")
    await pane.async_close(force=True)
    await tab.async_close(force=True)
    await app.async_refresh()
    assert app.get_session_by_id(pane.session_id) is None
    assert app.get_session_by_id(original_id) is not None
    assert len(app.get_window_by_id(window.window_id).tabs) == 1
    checks.append("Original close removes only owned new sessions and leaves the original real shell running")
    result = {"passed": True, "checks": checks, "python": sys.version.split()[0], "packages": {name: importlib.metadata.version(name) for name in ("iterm2", "protobuf", "websockets")}}
    Path(sys.argv[1]).write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")


iterm2.run_until_complete(main)
