
[![](https://img.shields.io/github/v/release/container-desktop/container-desktop?label=%20&style=for-the-badge&logo=github)](https://github.com/container-desktop/container-desktop/releases/latest)

# Container Desktop

Container Desktop is alternative to Docker for Desktop on Windows.
It is not a replacement and can only run on Windows versions that support WSL2. It is intended as a simple way to run the Docker daemon in its on WSL2 distribution and use the Docker CLI for windows to connect to it.

## Supported features

The solution is in its early days and supports the following features:

* Run Docker daemon in its own distribution
* Use Docker CLI on Windows to connect to the daemon
* You can use Windows paths as volumes, the container-desktop-proxy translates these paths to WSL2 paths local to the daemon distribution
* You can stop/start/restart and quit Container Desktop by right clicking on the icon in the system tray.

## Installation

**System requirements**

Container Desktop requires WLS2 which is supported on these Windows versions:

* Microsoft Windows 10, Version 1903 with Build 18362.1049 or higher
* Microsoft Windows 10, Version 1909 with Build 18363.1049 or higher
* Microsoft Windows 10, Version 2004 or higher (20H2,21H1, 21H2, ..)
* Microsoft Windows 11, any version or build

**Installation Steps**

1. Download the latest [Release](https://github.com/container-desktop/container-desktop/releases)
2. (optional) Validate the file checksum  with the values in sha256sum.txt

    ```powershell
    Get-FileHash .\ContainerDesktopInstaller.exe -Algorithm SHA256'
    ```

3. StartContainerDesktopInstaller.exe and click install to start the installation.

> Note: Windows Defender SmartScreen may pop-up and prevent StartContainerDesktopInstaller.exe from starting. When this is the case do the following: please select "More Info" and Select "Run Anyway".

## Build

**Pre-requisites**

* Microsoft Windows with Powershell
* [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
* Docker (either container-desktop or docker-for-desktop)

**Build**

1. Clone this repo 'git clone <https://github.com/container-desktop/container-desktop.git>'
2. Inside the container-desktop folder run '.\build.ps1'

## Unsupported

* Using Docker from other WSL2 distributions is not yet supported.
