ARG DOCKER_VERSION="20.10.8"
FROM golang:1.17 as builder
WORKDIR /src
COPY . .
RUN mkdir /dist
WORKDIR /src/cmd/container-desktop-proxy
RUN GOOS=windows GOARCH=amd64 go build -v -o /dist/container-desktop-proxy-windows-amd64.exe
RUN GOOS=linux GOARCH=amd64 go build -v -o /dist/container-desktop-proxy-linux-amd64

FROM docker:${DOCKER_VERSION}-dind
COPY deployment/wsl.conf /etc/
COPY deployment/wsl-init.sh /usr/local/bin/
RUN chmod +x /usr/local/bin/wsl-init.sh
RUN mkdir -p /usr/local/bin/cli-tools/linux && mkdir -p /usr/local/bin/cli-tools/windows && mkdir -p /usr/local/bin/proxy && \
    ln /usr/local/bin/docker /usr/local/bin/cli-tools/linux/docker
COPY dist/docker/* /usr/local/bin/cli-tools/windows/
COPY --from=builder dist/* /usr/local/bin/proxy/