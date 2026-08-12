@echo off
setlocal EnableExtensions
chcp 65001 >nul

set "ROOT=%~dp0"
set "BRIDGE_DIR=%ROOT%TikTokBridge"
set "BRIDGE_PORT=3000"

echo =======================================
echo     KHOI DONG ONG CHU MMO LIVE
echo =======================================
echo.

where node >nul 2>&1
if errorlevel 1 (
    echo [LOI] Khong tim thay Node.js.
    echo Hay cai Node.js 20 tro len: https://nodejs.org/
    goto :failed
)

if not exist "%BRIDGE_DIR%\package.json" (
    echo [LOI] Khong tim thay TikTokBridge\package.json.
    goto :failed
)

if not exist "%BRIDGE_DIR%\node_modules\express\package.json" (
    echo [1/3] Dang cai dat thu vien Node.js...
    pushd "%BRIDGE_DIR%"
    if exist package-lock.json (
        call npm ci
    ) else (
        call npm install
    )
    if errorlevel 1 (
        popd
        echo [LOI] npm install khong thanh cong.
        goto :failed
    )
    popd
) else (
    echo [1/3] Thu vien Node.js da san sang.
)

pushd "%BRIDGE_DIR%"
for /f "usebackq delims=" %%p in (`node -e "const e=require('./src/config/environment');e.loadEnvironmentFile();process.stdout.write(String(e.getServerSettings().port))"`) do set "BRIDGE_PORT=%%p"
popd
set "CONTROL_URL=http://127.0.0.1:%BRIDGE_PORT%/control.html"

call :bridge_is_ready
if defined BRIDGE_READY (
    echo [2/3] TikTok Bridge dang chay san tren cong %BRIDGE_PORT%.
    goto :launch_game
)

set "PORT_PID="
for /f "tokens=5" %%a in ('netstat -ano ^| findstr /r /c:":%BRIDGE_PORT% .*LISTENING" 2^>nul') do if not defined PORT_PID set "PORT_PID=%%a"
if defined PORT_PID (
    echo [LOI] Cong %BRIDGE_PORT% dang bi chuong trinh khac su dung ^(PID %PORT_PID%^).
    echo Launcher se KHONG tu tat chuong trinh khac de tranh mat du lieu.
    echo Hay dong chuong trinh do, sau do chay lai run.bat.
    goto :failed
)

echo [2/3] Dang khoi dong TikTok Bridge...
start "TikTok Bridge" /D "%BRIDGE_DIR%" cmd /k "npm start"

set "BRIDGE_READY="
for /l %%i in (1,1,30) do (
    if not defined BRIDGE_READY (
        call :bridge_is_ready
        if not defined BRIDGE_READY ping 127.0.0.1 -n 2 >nul
    )
)
if not defined BRIDGE_READY (
    echo [LOI] TikTok Bridge khong san sang sau 30 giay.
    echo Hay xem loi trong cua so "TikTok Bridge".
    goto :failed
)

:launch_game
echo [3/3] Dang khoi dong Game...
if not "%BRIDGE_PORT%"=="3000" (
    echo [CANH BAO] Ban game dung san chi ket noi cong 3000.
    echo Bridge va Control Panel van chay tren cong %BRIDGE_PORT%, nhung game se khong duoc mo.
    goto :open_control
)
if exist "%ROOT%Build\OngChuMMO_Live.exe" (
    start "" "%ROOT%Build\OngChuMMO_Live.exe"
) else if exist "%ROOT%Build\TIKTOK_LIVE_BAR.exe" (
    start "" "%ROOT%Build\TIKTOK_LIVE_BAR.exe"
) else (
    echo [CANH BAO] Khong tim thay file Game trong thu muc Build.
    echo Chay build.bat sau khi cai Unity 6, hoac mo UnityProject bang Unity Hub.
)

:open_control
start "" "%CONTROL_URL%"
echo.
echo Da khoi dong. Control Panel: %CONTROL_URL%
exit /b 0

:bridge_is_ready
set "BRIDGE_READY="
for /f "usebackq delims=" %%r in (`powershell -NoProfile -Command "try { $h = Invoke-RestMethod -Uri '%CONTROL_URL:control.html=api/health%' -TimeoutSec 2; if ($h.status -eq 'ok' -and $h.appId -eq 'ongchu-mmo-live-bridge') { 'YES' } } catch {}"`) do set "BRIDGE_READY=%%r"
exit /b 0

:failed
echo.
pause
exit /b 1
