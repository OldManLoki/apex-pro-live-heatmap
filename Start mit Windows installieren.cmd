@echo off
set "STARTUP=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup"
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$w=New-Object -ComObject WScript.Shell;$s=$w.CreateShortcut([IO.Path]::Combine($env:APPDATA,'Microsoft\Windows\Start Menu\Programs\Startup','Apex Pro Live Heatmap.lnk'));$s.TargetPath=[IO.Path]::Combine('%~dp0','ApexProHeatmap.exe');$s.WorkingDirectory='%~dp0';$s.Save()"
echo Autostart wurde eingerichtet.
pause
