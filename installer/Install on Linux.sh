#!/usr/bin/env bash
# Double-click (or run in a terminal) to install DewDrop, and SMAPI if needed.
here="$(cd "$(dirname "$0")" && pwd)"
cd "$here" || exit 1
run=(bash "$here/internal/install.sh" --from-launcher)

if [ -t 0 ]; then
    exec "${run[@]}" "$@"
fi

# Opened from a file manager without a terminal: open one to run the installer in. Some terminals don't start in
# this folder, hence the full path to the script.
has() { command -v "$1" >/dev/null 2>&1; }
if has xdg-terminal-exec; then exec xdg-terminal-exec "${run[@]}"
elif has ptyxis; then exec ptyxis -- "${run[@]}"
elif has kgx; then exec kgx -- "${run[@]}"
elif has gnome-terminal; then exec gnome-terminal -- "${run[@]}"
elif has konsole; then exec konsole -e "${run[@]}"
elif has xfce4-terminal; then exec xfce4-terminal -x "${run[@]}"
elif has mate-terminal; then exec mate-terminal -x "${run[@]}"
elif has terminator; then exec terminator -x "${run[@]}"
elif has tilix; then exec tilix -e "$(printf '%q ' "${run[@]}")"
elif has kitty; then exec kitty "${run[@]}"
elif has alacritty; then exec alacritty -e "${run[@]}"
elif has wezterm; then exec wezterm start -- "${run[@]}"
elif has foot; then exec foot "${run[@]}"
elif has x-terminal-emulator; then exec x-terminal-emulator -e "${run[@]}"
elif has xterm; then exec xterm -e "${run[@]}"
fi
message="Couldn't find a terminal to run the DewDrop installer in. Open a terminal in this folder and run: bash 'Install on Linux.sh'"
notify-send --app-name="DewDrop installer" --urgency=critical "$message" 2>/dev/null || echo "$message"
exit 1
