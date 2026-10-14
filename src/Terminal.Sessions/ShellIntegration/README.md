# Actual upstream shell integration

iterm2.bash and iterm2.zsh copied from gnachman/iTerm2-shell-integration
commit bcd52317a70f1a489b86fcfd1ee97df155741092, matching the pinned iTerm2
repository's gitlink. Source headers grant GPLv2-or-later (elect GPLv3); bash also
includes bash-preexec's own MIT notice. Retain all headers and the upstream
GPLv2 text under licenses. Aster wrappers are GPL-3.0-or-later. The zsh script's
terminal guard was adapted to permit AsterTerminal; the change is marked in its header.

Automatic integration is opt-in per local bash/zsh profile. Wrappers preserve
user startup configuration without editing any dotfiles. Unsupported commands
are rejected explicitly; Windows/fish/tcsh integration is not claimed.
Session metadata and OSC marks are handled by the iTerm2 adapter/terminal engine.
Shell utilities, SCP, triggers, AI or host-side commands from output are not enabled.
