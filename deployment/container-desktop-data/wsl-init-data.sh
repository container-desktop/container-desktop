#!/bin/sh
mkdir -p /mnt/wsl/container-desktop-data
mount --bind / /mnt/wsl/container-desktop-data
tail -f < /dev/null