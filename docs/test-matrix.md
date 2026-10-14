# Verification matrix — 2026-10-14

Local environment: Apple Silicon arm64, macOS 26.7 (Darwin 25.6.0), .NET SDK 10.0.402, runtime 10.0.13, Avalonia 12.1.4. System cc/clang builds native helper. `/bin/sh`, user zsh and system vim available; tmux unavailable.

| Area | macOS arm64 | Windows | Linux |
|---|---|---|---|
| Engine unit regressions | PASS | Runtime pending | Runtime pending |
| Real tty/commands/UTF-8 formatting | PASS | ConPTY test defined, SKIPPED locally | Pending |
| PTY resize/stty, Ctrl-C, exit code 7 | PASS | Pending (test subset defined) | Pending |
| Independent sessions and helper reaping | PASS | Pending | Pending |
| vim alternate screen, insertion, resize, shell return | PASS | Pending | Pending |
| Avalonia event input → real shell, copy/paste, search, resize → stty | PASS, headless UI | Pending | Pending |
| Tabs/panes/close/whole-window cleanup | PASS, headless real processes; native shortcut observations | Pending | Pending |
| Native desktop launch/input | PASS, actual zsh output; individual native key events executed pwd; screenshots | Pending | Pending |
| Physical keyboard, native clipboard, IME, mouse, DPI, light theme | Pending; bulk AX text insertion unreliable, native key events verified | Pending | Pending |
| tmux/less/SSH/htop | Pending (tmux missing) | Pending | Pending |
| Release build / local framework-dependent publish | PASS | Pending | Pending |
| Self-contained installer, signed/notarized artifacts, public downloads | Pending | Pending | Pending |
| Startup/memory/throughput/resize benchmarks | Not measured | Not measured | Not measured |

## Exact commands and outcomes

Commands were run from repository root. `.research/dotnet/dotnet` is the locally installed SDK executable; use `dotnet` with SDK 10.0.402 elsewhere.

```sh
DOTNET_CLI_TELEMETRY_OPTOUT=1 .research/dotnet/dotnet build Aster.slnx
# Initial build failed MSB3030 (helper-copy path), repaired. Later test builds succeeded.
DOTNET_CLI_TELEMETRY_OPTOUT=1 .research/dotnet/dotnet test Aster.slnx -c Release --logger 'trx;LogFileName=first-slice.trx' --results-directory artifacts/test-results
# Latest run: 19 passed, 1 Windows-only test skipped, 0 failed, 20 total.
DOTNET_CLI_TELEMETRY_OPTOUT=1 .research/dotnet/dotnet build Aster.slnx -c Release --no-restore
# Success, 0 warnings, 0 errors.
DOTNET_CLI_TELEMETRY_OPTOUT=1 .research/dotnet/dotnet publish src/App -c Release -r osx-arm64 --self-contained false -o artifacts/osx-arm64
# Success; local developer payload requiring .NET 10 runtime, not public release.
DOTNET_CLI_TELEMETRY_OPTOUT=1 .research/dotnet/dotnet restore Aster.slnx --locked-mode
# Success.
DOTNET_CLI_TELEMETRY_OPTOUT=1 .research/dotnet/dotnet publish src/App -c Release -r osx-arm64 --self-contained false -o artifacts/osx-arm64-final
# Success; final local payload launched, native application name Aster Terminal.
DOTNET_ROOT='/Users/agastya/Documents/ChatGPT/alvonia real/.research/dotnet' src/App/bin/Debug/net10.0/Aster.Terminal
# Native window and real zsh started; screenshots taken via cua-driver.
```

Versioned TRX evidence and dependency inventory are under `docs/evidence/`. CI jobs are configured for three OSes but have not run: configuration is not passing-CI evidence. Benchmarks and native physical-input tests remain required before broader support claims.
