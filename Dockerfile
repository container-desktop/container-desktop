ARG DOCKER_VERSION="20.10.12"
FROM docker:${DOCKER_VERSION}-dind
COPY deployment/wsl.conf /etc/
COPY deployment/wsl-init.sh /usr/local/bin/
RUN cp /usr/local/bin/docker-proxy /usr/local/bin/docker-proxy-org
COPY deployment/docker-proxy /usr/local/bin/docker-proxy-shim
COPY deployment/wsl-distro-*.sh /distro/
RUN apk add netcat-openbsd
COPY dist/docker/docker-compose /usr/local/bin/docker-compose
COPY dist/docker/linux/cli-plugins /usr/libexec/docker/cli-plugins/
COPY dist/docker/docker-compose /usr/libexec/docker/cli-plugins/docker-compose
ADD https://github.com/janeczku/go-dnsmasq/releases/download/1.0.7/go-dnsmasq-min_linux-amd64 /usr/local/bin/dns-forwarder
RUN chmod +x /usr/local/bin/wsl-init.sh && \
    chmod +x /distro/wsl-distro-init.sh && \
    chmod +x /distro/wsl-distro-rm.sh && \
    chmod +x /usr/local/bin/docker-proxy && \
    chmod +x /usr/local/bin/docker-compose && \
    chmod +x /usr/local/bin/dns-forwarder && \
    find /usr/libexec/docker/cli-plugins -type f -exec chmod +x {} \;
RUN mkdir -p /usr/local/bin/cli-tools/cli-plugins && \
    mkdir -p /etc/docker && \
    ln /usr/local/bin/docker /usr/local/bin/cli-tools/docker && \
    ln /usr/local/bin/docker-compose /usr/local/bin/cli-tools/docker-compose && \
    find /usr/libexec/docker/cli-plugins -type f -exec sh -c 'ln {} /usr/local/bin/cli-tools/cli-plugins/$(basename {})' \;
COPY dist/container-desktop-proxy-linux-amd64 /proxy/container-desktop-proxy
RUN chmod +x /proxy/container-desktop-proxy