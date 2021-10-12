# FAQ
## Where does Container Desktop record errors and events during installation and runtime?

Container Desktop uses Windows Event Log to record errors and events during installation and runtime. Use the following step to access the logs with Windows Event Viewer.

1. Open a command prompt.
2. Type eventvwr and press enter.
3. Expand the "Windows Logs" and select "Application" logs.
4. Filter the application for "Container Desktop" related events by Configuring "Filter current log..."
   1. Select Event sources "Container Desktop" and "Container Desktop Installer" and confirm by clicking "ok"
   2. Now only container desktop related events are shown.

## Why does Windows Defender Smart Screen prevents Container Desktop form downloading and starting?

Windows Defender SmartScreen may pop-up and prevent StartContainerDesktopInstaller.exe from downloading and installation. This happens as our software is currently not signed by an EV Code Signing Certificate, which can only be acquired by a registered business entity (which we are not).

**When downloading with Microsoft Edge**: SmartScreen may pop-up with the warning "ContainerDesktopInstaller.exe was blocked because it could harm your device". Please select "keep", then again a warning is shown, please select "Show More" and proceed with the download.

**When installing**: Windows Defender SmartScreen may pop-up and prevent StartContainerDesktopInstaller.exe to start. When this is the case do the following: please select "More Info" and Select "Run Anyway".

