# Find in terminal output

Press Cmd+F on macOS or Ctrl+Shift+F on Windows/Linux. Type text to search the
active pane's retained output. Literal, case-insensitive matching is the default.
The compact bar offers:

- **Aa**: match case.
- **aA**: enable smart case; queries with uppercase letters match exactly, while
  all-lowercase queries remain case-insensitive.
- **`.*`**: use regular expressions.
- **Word**: match whole Unicode words (letters, digits, underscores and marks).
- **↑ / ↓**, **Shift+Enter / Enter**: previous / next, wrapping through results.
- **Escape / ×**: clear highlights, close Find and return focus to the terminal.

Smart Case is a current-working-tree addition, not part of the published v0.13.0
packages. Current-source runtime verification is on macOS; Windows/Linux
acceptance is pending.

The count shows the selected result and the captured total. The current match
has a distinct highlight. Find leaves the user's terminal selection/clipboard
independent. Each pane owns and cancels its search; changing panes clears the old
highlights and searches the new pane with the current text/options.

Typing/options debounce for 150 ms; matching runs off the UI thread. Enter and
navigation refresh changed output. Continuous output does not run a full search
on every frame: the status reports when the captured count needs refresh. Logical
cell anchors retain the selected match across resize/reflow when it remains in
history. Reset, alternate-buffer changes or eviction invalidate stale positions.

Regex is .NET syntax, with case-insensitive matching unless Aa is selected or
smart case is enabled and the query contains uppercase letters. Soft wraps form
one logical line; hard newlines separate matches. NFC-normalized text
allows composed/decomposed queries to find the same displayed grapheme. A match
cannot split a combining, wide or emoji cell. Regex zero-length matches are not
selected. Trailing default blank cells at a hard line end are omitted. ICU-only
syntax, cross-hard-line regex and output filtering are outside this subset.
Version 0.9.0 applies this same bar to explicit native tmux windows;
it searches output retained since attach. Existing tmux server history outside
the hydrated viewport is not imported or searched. Switching tmux panes/tabs
rebinds Find; detaching cancels it while leaving server shells alive. Final native Unix package
acceptance and local display limitations are tracked in the verification matrix.

Searches cap at the newest one million cells / 20,000 physical rows, 4 MiB of
normalized text, 65,536 UTF-16 units per logical line, 1,024 query units and 10,000
matches. A truncated logical prefix is skipped, preserving regex anchor meaning.
Regex has a 25 ms match timeout and the matcher checks a 300 ms overall budget.
These are work limits, not a hard real-time latency guarantee. Time/cap warnings
identify partial results; an incomplete total is never presented as exhaustive.
Invalid patterns report an error in the bar. Hover over a clipped status to read
its full text.

Native final-package acceptance verifies actual shell output, regex/case/word,
Unicode navigation, malformed patterns and compact layout. Runtime support claims
and current evidence are recorded in test-matrix.md.
