@echo off
rem --- Build WindscribeChain.exe (no extra tools needed) ---
cd /d "%~dp0"
set FW=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319
set CSC=%FW%\csc.exe
"%CSC%" /nologo /target:winexe /platform:anycpu /codepage:65001 /optimize+ /out:WindscribeChainMini.exe /r:"%FW%\WPF\PresentationFramework.dll" /r:"%FW%\WPF\PresentationCore.dll" /r:"%FW%\WPF\WindowsBase.dll" /r:"%FW%\System.Xaml.dll" /r:"%FW%\System.dll" /r:"%FW%\System.Core.dll" /r:"%FW%\System.Web.Extensions.dll" /r:"%FW%\System.Net.Http.dll" app-mini.cs
if %errorlevel% neq 0 (
    echo.
    echo [FAIL] build errors above
) else (
    echo.
    echo [OK] WindscribeChain.exe built
)
