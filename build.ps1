# This script is dependent on:
# - dotnet 6.0 SDK
# - docker
# This script must run on Windows because the application is a Windows application.
$DOCKER_VERSION="20.10.19"
$DOCKER_COMPOSE_VERSION="v2.11.2"
# clean or create dist folder
if ((Test-Path dist/)) {
    Remove-Item dist/* -Recurse
}
else {
    New-Item dist -ItemType Directory
}

dotnet clean .\container-desktop\container-desktop.sln

# Build tools image
docker build -t container-desktop-tools:build --build-arg "DOCKER_VERSION=$DOCKER_VERSION" tools/container-desktop-tools/
# Download and extract docker cli to /dist/docker
docker run --rm -v "$($PWD):/src" container-desktop-tools:build sh -c "curl -LO https://download.docker.com/win/static/stable/x86_64/docker-$DOCKER_VERSION.zip && unzip -o docker-$DOCKER_VERSION.zip -x docker/dockerd.exe -d /src/dist"
# Extract Linux docker cli and plugins 
docker run --rm -v "$($PWD):/src" container-desktop-tools:build sh -c "mkdir /src/dist/docker/linux && cp -R /usr/libexec/docker/cli-plugins /src/dist/docker/linux"
# Download docker-compose to /dist/docker
docker run --rm -v "$($PWD):/src" container-desktop-tools:build sh -c "curl -L -o /src/dist/docker/docker-compose.exe https://github.com/docker/compose/releases/download/$DOCKER_COMPOSE_VERSION/docker-compose-Windows-x86_64.exe"
docker run --rm -v "$($PWD):/src" container-desktop-tools:build sh -c "curl -L -o /src/dist/docker/docker-compose https://github.com/docker/compose/releases/download/$DOCKER_COMPOSE_VERSION/docker-compose-linux-x86_64"
# Build proxy for Windows and Linux and copy to /dist
docker run --rm -v "$($PWD):/go/src" -w /go/src/cmd/container-desktop-proxy -e GOOS=windows -e GOARCH=amd64 golang:1.17 go build -v -o /go/src/dist/container-desktop-proxy-windows-amd64.exe
docker run --rm -v "$($PWD):/go/src" -w /go/src/cmd/container-desktop-proxy -e GOOS=linux -e GOARCH=amd64 golang:1.17 go build -v -o /go/src/dist/container-desktop-proxy-linux-amd64
# build port-forwarder for Windows and copy to /dist
docker run --rm -v "$($PWD):/go/src" -w /go/src/cmd/container-desktop-port-forwarder -e GOOS=windows -e GOARCH=amd64 golang:1.17 go build -v -o /go/src/dist/container-desktop-port-forwarder.exe
# Build distro image
docker build -t container-desktop:build --build-arg DOCKER_VERSION="$DOCKER_VERSION" .
# Create WSL distro from the distro image and copy to /dist
docker create --name cdbuild container-desktop:build
docker run --rm -v "$($PWD):/src" --privileged -v //var/run/docker.sock:/var/run/docker.sock container-desktop-tools:build sh -c "docker export cdbuild | gzip > /src/dist/container-desktop-distro.tar.gz"
docker rm cdbuild
# Build data distro image
docker build -t container-desktop-data:build .\deployment\container-desktop-data
# Create WSL distro from the data distro image and copy to /dist
docker create --name cddatabuild container-desktop-data:build
docker run --rm -v "$($PWD):/src" --privileged -v //var/run/docker.sock:/var/run/docker.sock container-desktop-tools:build sh -c "docker export cddatabuild | gzip > /src/dist/container-desktop-data-distro.tar.gz"
docker rm cddatabuild
# Publish and zip App to /dist
dotnet publish -c Release .\container-desktop\ContainerDesktop\ContainerDesktop.csproj
docker run --rm -v "$($PWD):/src" -w /src/container-desktop/ContainerDesktop/bin/Release/net6.0-windows10.0.18362.0/win-x64/publish container-desktop-tools:build zip -r9 /src/dist/container-desktop.zip .
# Publish installer
dotnet publish -c Release .\container-desktop\Installer\Installer.csproj -o dist