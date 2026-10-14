# Unix PTY helper

`dt-pty-host.c` adapted from Devolutions Terminal commit
`fa23083f1bda1a60fe2b322c6cfab4a616af9edb`, `native/linux-pty/dt-pty-host.c`,
2026-10-14. Copyright Devolutions Inc.; MIT notice in `licenses/Devolutions-MIT.txt`.
This separate native process calls forkpty and exec, relays bounded input frames
and terminal resize requests, and closes the shell process group on EOF.
Our build script replaces upstream's Zig/PowerShell build pipeline with host cc.
Build on the target OS and architecture; no cross-OS publish claim.

Modified 2026-10-14: Linux close enumerates `/proc` and signals only members of
the PTY session created by forkpty. Background jobs have separate process groups
and otherwise kept the PTY alive until the managed adapter's timeout. Tests verify
shell and child exit, and hosted Linux acceptance passes with the repair.
