# View installation log and diagnose run-time behavior

This page contains information on how to view the installation log events and how to diagnose run-time behavior of Container Desktop.

## View installation log events

Container Desktop uses Windows Event Log to record errors and events during installation. Use the following step to access the logs with Windows Event Viewer.

1. Open a command prompt or use the searh box in your taskbar. 
2. Type eventvwr and press enter.
3. Expand the “Windows Logs” and select “Application” logs.
4. Filter the application for “Container Desktop” related events by Configuring “Filter current log…”
5. Select Event sources “Container Desktop” and “Container Desktop Installer” and confirm by clicking “ok”
6. Now only container desktop related events are shown.

## Diagnose run-time behavior

Container Desktop provides a log stream to diagnose run-time behavior. You can access the log stream via the system tray application by selecting the option ***View log stream***. In the log stream view all run-time behavior events of container desktop are logged as structured JSON format.

![Example](../static/img/container-desktop-log-stream-runtime.png)
