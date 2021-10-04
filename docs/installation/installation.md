# Installation

## How to install?

1. Download the latest [ContainerDesktopInstaller](https://github.com/container-desktop/container-desktop/releases/latest) from GitHub releases.
2. (optional) Validate the file checksum with the values in sha256sum.txt with the PowerShell command: 
    ```
    Get-FileHash .\ContainerDesktopInstaller.exe -Algorithm SHA256'
    ```
3. StartContainerDesktopInstaller.exe and click install to start the installation.

>Note: Windows Defender SmartScreen may pop-up and prevent StartContainerDesktopInstaller.exe from starting. When this is the case do the following: please select "More Info" and Select "Run Anyway".

## System requirements

Container Desktop requires WLS2 which is supported on these Windows versions:

- Microsoft Windows 10, Version 1903 with Build 18362.1049 or higher
- Microsoft Windows 10, Version 1909 with Build 18363.1049 or higher
- Microsoft Windows 10, Version 2004 or higher (20H2,21H1, 21H2, ..)
- Microsoft Windows 11, any version or build