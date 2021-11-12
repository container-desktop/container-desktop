# Overview
## What is Container Desktop? 

Container Desktop is an open-source alternative to Docker Desktop. It enables you to build, push, pull and run Linux containers on Microsoft Windows, by providing a seamless integration with Docker Engine Community Edition running on Windows Subsystem for Linux. The Container Desktop bundles the open-source docker and docker-compose cli clients for a native and simple user experience.
## How it works?

![](static/img/container-desktop-overview.png)

Container Desktop Proxy handles the communication with the Docker Engine running in container-desktop distribution within Windows System Linux v2 (WSL2). The proxy enables native docker experience on Microsoft Windows, where it translates Windows Paths to WSL2 paths local to the  container-desktop distribution.

With the Container Desktop System Tray Application you can manage the behavior of container-desktop. You can start, stop and restart the Container Desktop Proxy on the Windows Host or Enable or Disable a Docker Engine proxy in any available WSL2 distribution.
Container Desktop installs the docker and the docker-compose command line interface tools.

> Note: Container Desktop will not support Windows Containers.

## Why Container Desktop?

- Free and open-source;
- Easy and hassle free installation experience;
- Plain and simple, no unnecessary bells and whistles;
