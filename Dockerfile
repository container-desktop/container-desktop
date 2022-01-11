ARG DOCKER_VERSION="20.10.12"
FROM docker:${DOCKER_VERSION}-dind
COPY deployment/wsl.conf /etc/
COPY deployment/wsl-init.sh /usr/local/bin/
RUN mv /usr/local/bin/docker-proxy /usr/local/bin/docker-proxy-org
COPY deployment/docker-proxy /usr/local/bin/
COPY deployment/wsl-distro-*.sh /distro/
RUN apk add netcat-openbsd
COPY dist/docker/docker-compose /usr/local/bin/docker-compose
RUN chmod +x /usr/local/bin/wsl-init.sh && \
    chmod +x /distro/wsl-distro-init.sh && \
    chmod +x /distro/wsl-distro-rm.sh && \
    chmod +x /usr/local/bin/docker-proxy && \
    chmod +x /usr/local/bin/docker-compose
RUN mkdir -p /usr/local/bin/cli-tools && \
    ln /usr/local/bin/docker /usr/local/bin/cli-tools/docker && \
    ln /usr/local/bin/docker-compose /usr/local/bin/cli-tools/docker-compose
COPY dist/container-desktop-proxy-linux-amd64 /proxy/container-desktop-proxy
RUN chmod +x /proxy/container-desktop-proxy