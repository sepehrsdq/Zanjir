@echo off
title Windscribe Chain
cd /d "%~dp0"
if exist WindscribeChainMini.exe (
    start "" "%~dp0WindscribeChainMini.exe"
) else if exist WindscribeChain.exe (
    start "" "%~dp0WindscribeChain.exe"
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0ChainGUI.ps1"
)
