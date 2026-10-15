# iTerm2 Python API core implementation plan

Goal: opt-in authenticated local scripting using iTerm2's actual protobuf /
WebSocket protocol and pinned original Python client. Prove real session listing,
tab/pane creation, input, screen reads and close against real shells. Unsupported
RPCs return explicit errors; do not advertise full API parity.

Architecture: copy upstream proto/api.proto from pinned
069b6e0dd8f3f0d82fc08ea5d294d28f7c5b986e with attribution and namespace-only
adaptation. Google.Protobuf 3.36.2 serializes messages; build-only Grpc.Tools
2.84.0 generates C# into obj (no handwritten protobuf codec). Both versions and
licences verified at official NuGet pages on 2026-10-15. Kestrel from pinned
ASP.NET Core 10.0.13 handles WebSocket framing; keep networking/auth/protocol in
Sessions and dispatch actual desktop actions through a small application target.
No HTTP UI, account, telemetry or external service is introduced.

The server is disabled by default. Bind only numeric IPv4 loopback, default port
1912 with a configurable validated alternative. Require original subprotocol,
original local-client Origin and a random 256-bit cookie in its original header.
Store endpoint/cookie only in a private current-user file (Unix directory 0700,
file 0600); never in exported settings or terminal output. Scripts are explicitly
trusted to read sessions/send input. Browser-origin clients and unauthenticated
requests are rejected. Bound clients, headers, binary frames, request rate,
subscriptions, UI resources and outgoing queues; disconnect slow clients.

Alternatives: handwritten HTTP/protobuf; custom Python-only API; original wire
protocol using mature components. Choose the original wire protocol with mature
components. Keep runtime/build licences, corresponding source and payload audits
current. ASP.NET framework/runtime notices must accompany self-contained builds;
Grpc.Tools remains build-only and must not enter app payloads.

1. Add pinned protocol/build packages and audit package licences/source metadata.
   Add Kestrel server and private credential lifecycle, strict authentication,
   limits and a target interface. Test handshake rejection and bounded frames.
2. Add stable UI session/window/tab identifiers and thin wrappers around existing
   tab/split/close operations. Implement core responses, session/layout/focus
   notifications needed by async_get_app, and actual text viewport extraction.
3. Add opt-in settings, app lifecycle and a Python launcher that reads the private
   endpoint file. Default original-client transport works at 1912; launcher may
   redirect only transport for a custom port. Never probe/control another running
   iTerm2 instance. Local tests use a uniquely owned ephemeral endpoint.
4. Run the pinned upstream Python client from a fixture: async_get_app, create tab,
   split, send a shell-local command, read real Unicode output, list updated panes
   and close owned sessions. Record any unsupported APIs/transport adaptations.
5. Verify on all three OSes, native final packages and local Retina; audit new
   runtime notices and payload; update parity/migration/test evidence honestly.
