# Profile output rules

Open Settings → Profiles → choose a profile → Triggers. Add a rule, enter its
name and regular expression, choose Highlight or Bookmark, and save preferences.
Changes apply to existing panes using that profile and to new sessions.

Examples:

| Pattern | Action | Use |
|---|---|---|
| `(?i)\b(error\|failed)\b` | Highlight | Colour error words, ignoring case |
| `^BUILD COMPLETE$` | Bookmark | Return to a build's completion line |
| `\bHTTP [45][0-9]{2}\b` | Highlight | Mark HTTP error status text |

Patterns use .NET's nonbacktracking regular expressions. Lookarounds,
backreferences and other unsupported constructs are rejected when saving.
Colours use #RRGGBB. Rules are opt-in; profiles have no rules by default.

Matching uses rendered logical lines when the shell emits a line feed, including
soft wrapping, Unicode and ANSI-coloured text. Escape payloads and overwritten
carriage-return text do not become matches. Partial progress lines without a
newline and alternate-screen programs are excluded. This is a subset of iTerm2's
trigger behavior and does not import its plist rules or support its full regex
and action set.

After a rule fires, the pane shows **Triggered lines**. Open it, select a record,
and choose **Go to line** (or double-click). Logical anchors follow resize and
scrollback movement; a line removed by the configured history limit is reported
as expired. Clear history removes the pane's records. Closing/resetting the pane
clears them too. Existing text colours remain until overwritten/cleared by the
terminal; disabling a rule does not recolour old output.

Rules cannot send text, run commands, open files/URLs, change the clipboard or
post system notifications. Output remains untrusted terminal data.

Limits: 32 rules per profile, 512-character patterns, 4096 UTF-16 units per
logical line (oversize or evicted prefixes are skipped), at most 32 highlighted
matches per rule per line and one history record for that rule/line. Each pane
retains 200 records. A Feed evaluates at most its first 512 line-feed candidates;
a 25 ms matching budget after the first rule evaluation can skip rules during output bursts, reported in Triggered lines. A 100 ms regex timeout pauses that rule until its configuration changes.
Wrapped-line capture maps retained history and can allocate more than ordinary
one-row matching. These limits protect interactive terminal use; this is not a
lossless log monitor.
