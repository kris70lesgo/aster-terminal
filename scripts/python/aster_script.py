#!/usr/bin/env python3
"""Run an existing iterm2 Python script against Aster's opt-in local API.

Only transport and authentication are adapted. Session/window/RPC APIs come from
upstream iterm2. This never discovers an iTerm2 socket or invokes AppleScript.
"""
from __future__ import annotations
import argparse
import json
import os
from pathlib import Path
import runpy
import stat
import sys


def default_endpoint() -> Path:
    if sys.platform == "darwin":
        root = Path.home() / "Library/Application Support/Aster Terminal"
    elif sys.platform == "win32":
        root = Path(os.environ["LOCALAPPDATA"]) / "Aster Terminal"
    else:
        root = Path(os.environ.get("XDG_CONFIG_HOME", Path.home() / ".config")) / "aster-terminal"
    return root / "api/endpoint.json"


def install_transport(path: Path) -> None:
    import iterm2
    import iterm2.connection
    from websockets.legacy.client import connect

    metadata = path.stat()
    if metadata.st_size > 4096 or not stat.S_ISREG(metadata.st_mode):
        raise ValueError("Invalid Aster endpoint file")
    if os.name != "nt" and (metadata.st_uid != os.getuid() or metadata.st_mode & 0o077):
        raise PermissionError("Aster endpoint must be private to the current user")
    endpoint = json.loads(path.read_text(encoding="utf-8"))
    port, cookie = endpoint["Port"], endpoint["Token"]
    if type(port) is not int or not 1024 <= port <= 65535:
        raise ValueError("Invalid Aster API port")
    if not isinstance(cookie, str) or len(cookie) != 64 or any(c not in "0123456789abcdefABCDEF" for c in cookie):
        raise ValueError("Invalid Aster credential")
    original = iterm2.connection.Connection

    class AsterConnection(original):
        def authenticate(self, force: bool) -> bool:
            # A stale credential fails the handshake. It never falls back to
            # upstream's AppleScript authentication or another app's credentials.
            return True

        def _remove_auth(self) -> None:
            pass

        def _get_connect_coro(self):
            return connect(
                f"ws://127.0.0.1:{port}/",
                origin="ws://localhost/",
                extra_headers={"x-iterm2-cookie": cookie, "x-iterm2-advisory-name": "Aster Python script"},
                subprotocols=["api.iterm2.com"],
                open_timeout=5,
                close_timeout=2,
                max_size=1024 * 1024,
                compression=None,
            )

    # Upstream run_until_complete/run_forever and async_create resolve this
    # module global. Their dispatcher, RPCs and session models are unmodified.
    iterm2.connection.Connection = AsterConnection
    iterm2.Connection = AsterConnection


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--endpoint", type=Path, default=default_endpoint())
    parser.add_argument("script", type=Path)
    parser.add_argument("arguments", nargs=argparse.REMAINDER)
    args = parser.parse_args()
    install_transport(args.endpoint)
    script = args.script.resolve()
    sys.argv = [str(script), *args.arguments]
    sys.path.insert(0, str(script.parent))
    runpy.run_path(str(script), run_name="__main__")


if __name__ == "__main__":
    main()
