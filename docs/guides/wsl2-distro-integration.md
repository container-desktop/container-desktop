# WSL2 Distro integration

This page contains information on how to configure Container Desktop integration in other WSL2 distributions.
Select in Container desktop an installed WSL 2 distribution for which you want to enable Docker integration, by navigating to: Settings > Resources > WSL2 Integration

![WSL2 Distro Integration](../static/img/container-desktop-wsl2-distro-integration.png)

To confirm that Container Desktop has been enabled in the WSL2 distro open a terminal session of WSL2 distribution (e.g. Ubuntu) and validate that docker-cli commands are available: ```docker --version``` or ```docker-compose```. After that you can list any running containers with ```docker ps``` this list the same running containers as running on the host. 
