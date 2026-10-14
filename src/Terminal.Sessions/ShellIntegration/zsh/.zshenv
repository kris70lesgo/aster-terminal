# Preserve user startup configuration without modifying their files.
_aster_user_zdotdir=$ASTER_USER_ZDOTDIR
ZDOTDIR=$_aster_user_zdotdir
[[ ! -r $ZDOTDIR/.zshenv ]] || source "$ZDOTDIR/.zshenv"
_aster_user_zdotdir=$ZDOTDIR
ZDOTDIR=$ASTER_INTEGRATION_ROOT/zsh
