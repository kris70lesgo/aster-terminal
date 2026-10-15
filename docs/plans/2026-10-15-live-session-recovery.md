# Live session recovery implementation plan

Goal: preserve actual PTY processes across UI crashes and explicit detach, then reconnect to the same shell.
Architecture: each opted-in pane uses an independent C# host process running the existing native transport. Authenticated current-user named pipes carry bounded framed IO, resize, detach/terminate and metadata. A private descriptor records the capability and profile identity, never environment secrets/output. A bounded in-memory VT transcript and ordered resize events restore the frontend; overflow checkpoints preserve the current text view with explicit recovery limits. The host answers terminal queries; replaying clients suppress protocol replies. Normal close terminates, connection loss detaches, detached expiry bounds orphans. Recovery is local, not remote scripting or an iTerm2 server protocol port.

1. Implement private catalog, framing, host launcher/server and reconnecting connection behind existing transport boundaries. Validate framing/auth and bound queues/transcripts.
2. Add app headless host mode; add keep-after-crash preference, startup recovery and detach-window action. Preserve existing normal-close cleanup semantics and direct-connection test mode.
3. Test real shells with live variables, same process identity, detached output, resize, exclusivity, rejected tokens, termination and host cleanup on each OS.
4. Add native crash/reopen acceptance using isolated fixtures; inspect actual GUI/runtime evidence, including Retina.
5. Continue other parity gaps after recovery is complete; document actual scope and recheck/audit final packages before publishing.

Pinned iTerm2 source references: iTermMonoServerJobManager.m / iTermMultiServerJobManager.m / iTermOrphanServerAdopter.m study process adoption and socket handshake. New implementation uses .NET pipes and existing MIT native transports; no upstream server or network code is copied. No new NuGet dependency.
