#!/bin/sh
mkdir -p /mnt/host/wsl/container-desktop/cli-tools
mkdir -p /mnt/host/wsl/container-desktop/certs
mkdir -p /mnt/wsl
mkdir -p /var/lib/docker
mount --bind /usr/local/bin/cli-tools /mnt/host/wsl/container-desktop/cli-tools
mount --bind /certs /mnt/host/wsl/container-desktop/certs
mount --bind /mnt/host/wsl /mnt/wsl
mount --bind /mnt/host/wsl/container-desktop-data/data /var/lib/docker
export DOCKER_TLS_CERTDIR=/certs
nohup /usr/local/bin/dockerd-entrypoint.sh &
c=0
while [ ! -S /var/run/docker.sock ]; do
    sleep 1
    c=$((c+1))
    if [ $c -eq 10 ]; then
        exit 1
    fi
done
echo "Docker daemon started."
mkdir -p $1
cp /certs/client/*.pem $1