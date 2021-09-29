# Container Desktop

Container Desktop is alternative to Docker for Desktop on Windows.
It is not a replacement and can only run on Windows versions that support WSL2. It is intended as a simple way to run the Docker daemon in its on WSL2 distribution and use the Docker CLI for windows to connect to it.

## Supported features

The solution is in its early days and supports the following features:
* Run Docker daemon in its own distribution
* Use Docker CLI on Windows to connect to the daemon
* You can use Windows paths as volumes, the container-desktop-proxy translates these paths to WSL2 paths local to the daemon distribution
* You can stop/start/restart and quit Container Desktop by right clicking on the icon in the system tray.

## Unsupported

* Using Docker from other WSL2 distributions is not yet supported.
