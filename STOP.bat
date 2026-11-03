@echo off
chcp 65001 >nul
title Windscribe Chain - STOP
echo Stopping chain...
taskkill /F /IM sing-box.exe >nul 2>&1
if %errorlevel%==0 (echo [OK] Stopped.) else (echo Not running.)
timeout /t 2 /nobreak >nul
