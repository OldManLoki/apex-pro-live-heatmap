@echo off
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
"%CSC%" /nologo /target:winexe /optimize+ /out:"%~dp0ApexProHeatmap.exe" /reference:System.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll "%~dp0ApexHeatmapApp.cs"
if errorlevel 1 (
  echo Build fehlgeschlagen.
  pause
  exit /b 1
)
echo ApexProHeatmap.exe wurde erstellt.
pause

