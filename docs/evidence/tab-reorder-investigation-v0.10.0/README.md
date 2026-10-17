# First 0.10.0 candidate: retained Linux failures

Source a226a15 / run 39001432021. Two existing tmux fixtures failed:
ordinary tmux issued its one-shot stty before the inner pane PTY resized (23 80
reported instead of 34 105); Find checked a discovered third pane's initial
empty results before asynchronous startup selected the new tab and rebound Find.

The fix waits for the owned pane device's actual kernel dimensions, retaining
the exact shell size assertion, and waits for an actual completed new-pane Find
query before checking isolation. No production tmux/search behavior was changed.
New tab reordering tests passed in this candidate. The failed TRX is retained.
