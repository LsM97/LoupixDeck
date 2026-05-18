#!/usr/bin/env bash
# LoupixDeck Linux installer – distro-agnostic.
# Downloads the latest GitHub release binary, installs it system-wide,
# sets up udev rules, a desktop entry, and the .NET runtime if missing.
set -euo pipefail

REPO="LsM97/LoupixDeck"
ASSET_NAME="LoupixDeck-linux-x64.tar.gz"
INSTALL_DIR="/usr/local/lib/loupixdeck"
SYMLINK="/usr/local/bin/loupixdeck"
DESKTOP_FILE="/usr/share/applications/loupixdeck.desktop"
UDEV_RULES_FILE="/etc/udev/rules.d/99-loupixdeck.rules"
DOTNET_... (9621 bytes total)
[full output: ~/.local/share/rtk/tee/1779144061_curl.log]
