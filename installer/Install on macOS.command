#!/usr/bin/env bash
# Double-click to install DewDrop (and SMAPI if needed). If macOS says it can't be opened, click Done, then
# System Settings > Privacy & Security > Open Anyway. Help: https://github.com/friessssss/DewDrop#install
cd "$(dirname "$0")" || exit 1
xattr -r -d com.apple.quarantine . 2>/dev/null
exec bash ./internal/install.sh --from-launcher "$@"
