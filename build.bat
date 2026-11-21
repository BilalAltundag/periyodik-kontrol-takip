@echo off
rem Windowsta ek kurulum gerektirmeden derleme (.NET Framework 4.x ile gelen csc.exe).
rem Cikti: dist\PeriyodikKontrolTakip.exe
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist dist mkdir dist
"%CSC%" /nologo /codepage:65001 /optimize+ /target:winexe /out:dist\PeriyodikKontrolTakip.exe /win32icon:src\app.ico /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Web.Extensions.dll /r:System.IO.Compression.dll src\*.cs
if errorlevel 1 (echo Derleme basarisiz. & exit /b 1)
echo Derlendi: dist\PeriyodikKontrolTakip.exe
