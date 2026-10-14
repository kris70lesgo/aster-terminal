# Aster launch wrapper, GPL-3.0-or-later. Upstream integration remains unchanged.
if [[ ${ASTER_LOGIN_SHELL:-0} == 1 ]]; then
  [[ ! -r /etc/profile ]] || source /etc/profile
  if [[ -r $HOME/.bash_profile ]]; then source "$HOME/.bash_profile"
  elif [[ -r $HOME/.bash_login ]]; then source "$HOME/.bash_login"
  elif [[ -r $HOME/.profile ]]; then source "$HOME/.profile"
  fi
else
  [[ ! -r $HOME/.bashrc ]] || source "$HOME/.bashrc"
fi
source "$ASTER_INTEGRATION_ROOT/iterm2.bash"
