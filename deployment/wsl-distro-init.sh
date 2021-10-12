#!/bin/sh
distro="${1:-$WSL_DISTRO_NAME}"
mkdir -p /mnt/wsl/$distro
mount --bind / /mnt/wsl/$distro
readlink /usr/local/bin/docker || ln -s /mnt/wsl/container-desktop/cli-tools/docker /usr/local/bin/docker
readlink /usr/local/bin/docker-compose || ln -s /mnt/wsl/container-desktop/cli-tools/docker-compose /usr/local/bin/docker-compose
rm /var/run/docker.sock
/mnt/wsl/container-desktop/proxy/container-desktop-proxy \
  --listen-address unix:///var/run/docker.sock \
  --target-address https://localhost:2376 \
  --wsl-distro-name "$distro" \
  --tls-key /mnt/wsl/container-desktop/certs/client/key.pem \
  --tls-cert /mnt/wsl/container-desktop/certs/client/cert.pem \
  --tls-ca /mnt/wsl/container-desktop/certs/client/ca.pem &

c=0
while [ ! -S /var/run/docker.sock ]; do
    sleep 1
    c=$((c+1))
    if [ $c -eq 10 ]; then
        exit 1
    fi
done

chmod 777 /var/run/docker.sock

wait
