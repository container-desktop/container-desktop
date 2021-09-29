ARG DOCKER_VERSION="20.10.8"
FROM docker:${DOCKER_VERSION}-dind
COPY deployment/wsl.conf /etc/
COPY deployment/wsl-init.sh /usr/local/bin/
COPY deployment/wsl-distro-init.sh /distro/
RUN chmod +x /usr/local/bin/wsl-init.sh
RUN mkdir -p /usr/local/bin/cli-tools && \
    ln /usr/local/bin/docker /usr/local/bin/cli-tools/docker
COPY dist/container-desktop-proxy-linux-amd64 /usr/local/bin/
RUN chmod +x /usr/local/bin/container-desktop-proxy-linux-amd64