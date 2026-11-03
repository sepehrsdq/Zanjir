@echo off
rem --- شروع خودکار زنجیرهٔ ویندسکرایب (وظیفهٔ زمان‌بندی‌شده) ---
cd /d "%~dp0"
taskkill /F /IM sing-box.exe >nul 2>&1
start "" /min "bin\sing-box-1.14.4-windows-amd64\sing-box.exe" run -c "config.json"
