@echo off
setlocal enabledelayedexpansion

:menu
cls
echo ==========================================
echo               WSLMgr Build Tool
echo ==========================================
echo.
echo  [1] Clean Build
echo  [2] Build (Debug)
echo  [3] Publish (Release Single-File)
echo  [4] Exit
echo.
set "choice="
set /p choice="Enter choice (1-4): "

if "%choice%"=="" goto menu
if "%choice%"=="1" goto clean
if "%choice%"=="2" goto build
if "%choice%"=="3" goto publish
if "%choice%"=="4" goto exit
goto menu

:clean
echo.
echo Cleaning build files...
dotnet clean WSLMgr\WSLMgr.csproj -c Debug
dotnet clean WSLMgr\WSLMgr.csproj -c Release
if exist WSLMgr\bin rmdir /s /q WSLMgr\bin
if exist WSLMgr\obj rmdir /s /q WSLMgr\obj
del /f /q WSLMgr.exe 2>nul
del /f /q WSLMgr.pdb 2>nul
del /f /q D3DCompiler_47_cor3.dll 2>nul
del /f /q PenImc_cor3.dll 2>nul
del /f /q PresentationNative_cor3.dll 2>nul
del /f /q vcruntime140_cor3.dll 2>nul
del /f /q wpfgfx_cor3.dll 2>nul
echo.
echo Clean completed!
pause
goto menu

:build
echo.
echo Building project...
dotnet build WSLMgr\WSLMgr.csproj
echo.
echo Build completed!
pause
goto menu

:publish
echo.
echo Publishing release build...
dotnet publish WSLMgr\WSLMgr.csproj -c Release
if %ERRORLEVEL% equ 0 (
    echo.
    echo Copying publish output to root folder...
    copy /y WSLMgr\bin\Release\net8.0-windows\win-x64\publish\* .
    echo.
    echo Publish completed successfully!
) else (
    echo.
    echo Publish failed.
)
pause
goto menu

:exit
endlocal
