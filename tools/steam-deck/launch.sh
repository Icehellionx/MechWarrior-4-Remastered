#!/usr/bin/env bash
set -euo pipefail

case "${1:-}" in
    setup|play) mode=$1; shift ;;
    *) printf '%s\n' 'Usage: launch.sh {setup|play} [--log] STEAM_COMMAND...' >&2; exit 2 ;;
esac

log=false
if [[ "${1:-}" == --log ]]; then
    log=true
    shift
fi
if [[ $# -eq 0 || "$1" == '%command%' || "${HOME:-}" != /* ]]; then
    printf '%s\n' 'Use this script in a Steam shortcut with %command% at the end.' >&2
    exit 2
fi

export STEAM_COMPAT_DATA_PATH="$HOME/.local/share/mw4-remastered-deck-alpha"
(umask 077; mkdir -p -- "$STEAM_COMPAT_DATA_PATH")

if [[ "$mode" == play ]]; then
    export WINEDLLOVERRIDES="ddraw=b;d3dimm=b"
    export PROTON_USE_D7VK=0
fi

if "$log"; then
    export PROTON_LOG=1
    export PROTON_LOG_DIR="$STEAM_COMPAT_DATA_PATH/logs"
    (umask 077; mkdir -p -- "$PROTON_LOG_DIR")
fi

exec "$@"
