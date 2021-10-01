#!/bin/sh
distro="${1:-$WSL_DISTRO_NAME}"
pkill -f container-desktop-proxy
readlink /usr/local/bin/docker-compose && unlink /usr/local/bin/docker-compose
readlink /usr/local/bin/docker && unlink /usr/local/bin/docker
#umount /mnt/wsl/$distro
#rmdir /mnt/wsl/$distro