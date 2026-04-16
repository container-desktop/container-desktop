# FAQ

## Where does Container Desktop record errors and events during installation and at runtime?

Container Desktop uses Windows Event Log to record errors and events during installation and runtime. Use the following steps to access the logs with Windows Event Viewer.

1. Open a command prompt.
2. Type `eventvwr` and press Enter.
3. Expand **Windows Logs** and select **Application** logs.
4. Filter for Container Desktop related events by selecting **Filter Current Log…**
   1. Select Event sources **Container Desktop** and **Container Desktop Installer**, then click **OK**.
   2. Only Container Desktop related events are now shown.

## Why does Windows Defender SmartScreen prevent Container Desktop from downloading and starting?

Windows Defender SmartScreen may pop up and prevent ContainerDesktopInstaller.exe from downloading or starting. This happens because our software is currently not signed by an EV Code Signing Certificate, which can only be acquired by a registered business entity (which we are not).

**When downloading with Microsoft Edge:** SmartScreen may pop up with the warning "ContainerDesktopInstaller.exe was blocked because it could harm your device". Select **Keep**, then when the next warning appears, select **Show More** and proceed with the download.

**When installing:** Windows Defender SmartScreen may pop up and prevent ContainerDesktopInstaller.exe from starting. When this happens, select **More Info** and then **Run Anyway**.
