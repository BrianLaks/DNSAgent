@echo off
:: Check for Administrator privileges
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"
if '%errorlevel%' NEQ '0' (
    echo.
    echo ************************************************************
    echo   ERROR: Administrator Privileges Required
    echo ************************************************************
    echo.
    echo Please right-click this file and select "Run as Administrator".
    echo.
    pause
    exit /B
)

powershell.exe -ExecutionPolicy Bypass -File "%~dp0Toggle-DNS.ps1"
if %errorlevel% neq 0 (
    echo.
    echo ************************************************************
    echo   DNS Toggle failed with error code %errorlevel%
    echo ************************************************************
    pause
)
