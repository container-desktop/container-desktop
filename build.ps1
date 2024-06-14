# This script is dependent on:
# - dotnet 6.0 SDK
# - docker
# This script must run on Windows because the application is a Windows application.
$DOCKER_VERSION="26.1.4"
$DOCKER_COMPOSE_VERSION="v2.27.1"
$DOCKER_BUILDX_VERSION="v0.15.0"
$GO_VERSION="1.22"
function ExitOnFailure([string] $message, [string] $sha) {
    if (($LastExitCode -ne 0) -or (-not $?)) {
        $exitCode = $LastExitCode
        Write-Host $message
        if($sha) {
            docker kill $sha
        }
        Exit $exitCode
    }
}

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
ExitOnFailure("Failed to build tools image")

# Download and extract docker cli to /dist/docker
docker run --rm -v "$($PWD):/src" container-desktop-tools:build sh -c "curl -LO https://download.docker.com/win/static/stable/x86_64/docker-$DOCKER_VERSION.zip && unzip -o docker-$DOCKER_VERSION.zip -x docker/dockerd.exe -d /src/dist"
ExitOnFailure("Failed to download docker")
# Extract Linux docker cli and plugins 
docker run --rm -v "$($PWD):/src" container-desktop-tools:build sh -c "mkdir -p /src/dist/docker/linux && cp -R /usr/libexec/docker/cli-plugins /src/dist/docker/linux"
ExitOnFailure("Failed to copy linux cli and plugins")
# Download docker-compose to /dist/docker
docker run --rm -v "$($PWD):/src" container-desktop-tools:build sh -c "curl -L -o /src/dist/docker/docker-compose.exe https://github.com/docker/compose/releases/download/$DOCKER_COMPOSE_VERSION/docker-compose-Windows-x86_64.exe"
ExitOnFailure("Failed to download docker-compose for Windows")
docker run --rm -v "$($PWD):/src" container-desktop-tools:build sh -c "curl -L -o /src/dist/docker/docker-compose https://github.com/docker/compose/releases/download/$DOCKER_COMPOSE_VERSION/docker-compose-linux-x86_64"
ExitOnFailure("Failed to download docker-compose for Linux")
# Download buildx
docker run --rm -v "$($PWD):/src" container-desktop-tools:build sh -c "mkdir -p /src/dist/docker/cli-plugins"
docker run --rm -v "$($PWD):/src" container-desktop-tools:build sh -c "curl -L -o /src/dist/docker/cli-plugins/docker-buildx.exe https://github.com/docker/buildx/releases/download/$DOCKER_BUILDX_VERSION/buildx-$DOCKER_BUILDX_VERSION.windows-amd64.exe"
ExitOnFailure("Failed to download docker-buildx for Windows")
# Download WSL Kernel MSI
docker run --rm -v "$($PWD):/src" container-desktop-tools:build sh -c "curl -L -o /src/dist/wsl_update_x64.msi https://wslstorestorage.blob.core.windows.net/wslblob/wsl_update_x64.msi"
ExitOnFailure("Failed to download WSL Kernel MSI")
# Download dns-forwarder (go-dnsmasq)
docker run --rm -v "$($PWD):/src" container-desktop-tools:build sh -c "mkdir -p /src/dist/bin/"
ExitOnFailure("Failed to download dns-forwarder")
docker run --rm -v "$($PWD):/src" container-desktop-tools:build sh -c "curl -L -o /src/dist/bin/dns-forwarder https://github.com/janeczku/go-dnsmasq/releases/download/1.0.7/go-dnsmasq-min_linux-amd64"
ExitOnFailure("Failed to download dns-forwarder")
# Build proxy for Windows and Linux and copy to /dist
docker run --rm -v "$($PWD):/go/src" -w /go/src/cmd/container-desktop-proxy -e GOOS=windows -e GOARCH=amd64 golang:$GO_VERSION go build -v -o /go/src/dist/container-desktop-proxy-windows-amd64.exe
ExitOnFailure("Failed to build container-desktop-proxy for Windows")
docker run --rm -v "$($PWD):/go/src" -w /go/src/cmd/container-desktop-proxy -e GOOS=linux -e GOARCH=amd64 golang:$GO_VERSION go build -v -o /go/src/dist/container-desktop-proxy-linux-amd64
ExitOnFailure("Failed to build container-desktop-proxy for Linux")
# build port-forwarder for Windows and copy to /dist
docker run --rm -v "$($PWD):/go/src" -w /go/src/cmd/container-desktop-port-forwarder -e GOOS=windows -e GOARCH=amd64 golang:$GO_VERSION go build -v -o /go/src/dist/container-desktop-port-forwarder.exe
ExitOnFailure("Failed to build container-desktop-port-forwarder for Windows")
# Build distro image
docker build -t container-desktop:build --build-arg DOCKER_VERSION="$DOCKER_VERSION" .
ExitOnFailure("Failed to build container-desktop image")
# Create WSL distro from the distro image and copy to /dist
docker create --name cdbuild container-desktop:build
docker run --rm -v "$($PWD):/src" --privileged -v //var/run/docker.sock:/var/run/docker.sock container-desktop-tools:build sh -c "docker export cdbuild | gzip > /src/dist/container-desktop-distro.tar.gz"
ExitOnFailure("Failed to build container-desktop WSL distro")
docker rm cdbuild
# Build data distro image
docker build -t container-desktop-data:build .\deployment\container-desktop-data
ExitOnFailure("Failed to build container-desktop-data image")
# Create WSL distro from the data distro image and copy to /dist
docker create --name cddatabuild container-desktop-data:build
docker run --rm -v "$($PWD):/src" --privileged -v //var/run/docker.sock:/var/run/docker.sock container-desktop-tools:build sh -c "docker export cddatabuild | gzip > /src/dist/container-desktop-data-distro.tar.gz"
ExitOnFailure("Failed to build container-desktop-proxy WSL distro")
docker rm cddatabuild
# Publish and zip App to /dist
dotnet publish -c Release .\container-desktop\ContainerDesktop\ContainerDesktop.csproj
ExitOnFailure("Failed to build container-desktop")
docker run --rm -v "$($PWD):/src" -w /src/container-desktop/ContainerDesktop/bin/Release/net8.0-windows10.0.18362.0/win-x64/publish container-desktop-tools:build zip -r9 /src/dist/container-desktop.zip .
ExitOnFailure("Failed to zip container-desktop")
# Publish installer
dotnet publish -c Release .\container-desktop\Installer\Installer.csproj -o dist
ExitOnFailure("Failed to build container-desktop installer")