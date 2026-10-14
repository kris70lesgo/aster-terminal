# Migration report — first execution

**Aster Terminal is a functioning first slice of an iTerm2 behavior migration to C#/Avalonia. It is not competition-ready or a complete port.**

1. **Difficulty/completeness:** iTerm2's Objective-C/Swift parser, mutable screen state, Metal rendering, AppKit hierarchy and long-lived process servers do not map directly to a portable UI. We directly adapted semantic prompt status parsing and cwd/host metadata. Other terminal functionality currently comes from an established terminal component; advanced iTerm2 behaviors remain in the parity matrix.
2. **Application quality:** actual shells render and execute commands. There are independent tabs and both split directions, find, selection/copy/paste and resize. Native screenshot inspection confirms layout and pane borders; preferences, light theme, accessibility and physical-input coverage still need work.
3. **Cross-platform implementation:** shared Avalonia host, OS selection isolated in a factory, ConPTY versus native forkpty relay. Verified on macOS only. Windows/Linux CI and acceptance definitions exist but are not runtime evidence.
4. **Engineering quality:** pinned SDK/packages/source commits, lockfiles, bounded parser strings/input queues, async disposal and serialized layout mutation; 19 automated tests pass and a Windows-specific test is skipped. Real-process tests include vim, interrupt, resize, independent sessions and clean close. No performance parity claim.
5. **Usefulness/costs:** log records concrete choices and failures, including missing NuGet Unix helper and Headless/xUnit version mismatch. No tracked human hours or AI cost ledger exists, so costs are unmeasured. GPL mismatch is explained with upstream grant evidence. Licence inventory and final runtime payload notices need review before distribution.

Build/run instructions and migration synopsis belong in README for contest submission. Public GitHub repository, platform downloads, final matched before/after screenshots, finished write-up and form submission remain outstanding. Milestone A ends only with verified contest evidence; Milestone B continues deeper parity after that release.
