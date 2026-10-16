# Scripting lifecycle investigation

Reproduce the 0.8.0 local native scripting timeout / post-report exit stall with
fresh owned report directories. Preserve foreground and normal app processes;
never count a report without matching PID, final result and actual process exit.

Add fixture-only phase logs, Python progress and captured output on timeout,
plus main-loop return evidence. Distinguish client/RPC delay, server/window
cleanup, shutdown dispatch and native-loop return. Inspect the actual failed
phase before changing application behavior. Prefer correcting lifecycle ordering
or transport correlation at its owning layer; avoid forced process exit or
broad dependency upgrades that could conceal the failure.

Once reproduced, add a deterministic regression where possible, run full tests
and native repetitions including the failing ordered sequence. Record failures
and precise verification scope. A release fix requires final three-OS packages,
exact installed Mac checks and payload/rule audits; do not erase old failures.
