.PHONY = all build clean build-distro

DOCKER_VERSION=20.10.8

all: clean build

clean:
	rm -rf dist/*

dist/docker/docker.exe:
	curl -LO https://download.docker.com/win/static/stable/x86_64/docker-${DOCKER_VERSION}.zip 
	unzip docker-${DOCKER_VERSION}.zip -x docker/dockerd.exe -d dist
	rm docker-${DOCKER_VERSION}.zip

build: build-distro

build-distro: dist/docker/docker.exe
	docker build -t container-desktop:build --build-arg DOCKER_VERSION=${DOCKER_VERSION} .
	docker create --name cdbuild container-desktop:build
	docker export cdbuild | gzip > dist/container-desktop-distro.tar.gz
	docker cp cdbuild:/usr/local/bin/proxy/container-desktop-proxy-windows-amd64.exe dist/
	docker rm cdbuild
