#!/bin/sh
distro="${1:-$WSL_DISTRO_NAME}"
mkdir -p /mnt/wsl/$distro
mount --bind / /mnt/wsl/$distro
readlink /usr/local/bin/docker || ln -s /mnt/wsl/container-desktop/cli-tools/docker /usr/local/bin/docker
readlink /usr/local/bin/docker-compose || ln -s /mnt/wsl/container-desktop/cli-tools/docker-compose /usr/local/bin/docker-compose
exec /mnt/wsl/container-desktop/proxy/container-desktop-proxy \
  --listen-address unix:///var/run/docker.sock \
  --target-address https://localhost:2376 \
  --wsl-distro-name "$distro" \
  --tls-key /mnt/wsl/container-desktop/certs/client/key.pem \
  --tls-cert /mnt/wsl/container-desktop/certs/client/cert.pem \
  --tls-ca /mnt/wsl/container-desktop/certs/client/ca.pem
