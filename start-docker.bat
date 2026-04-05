@echo off
echo Starting Docker services...
powershell -ExecutionPolicy Bypass -File "%~dp0start-docker.ps1"
pause
