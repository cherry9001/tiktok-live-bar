@echo off
echo =======================================
echo     KHOI DONG ONG CHU MMO LIVE
echo =======================================

echo.
echo [1/2] Dang khoi dong Node Bridge...
cd TikTokBridge

REM Tat process cu neu con chay tren port 3000
for /f "tokens=5" %%a in ('netstat -ano ^| findstr ":3000.*LISTENING" 2^>nul') do (
    echo     Phat hien port 3000 dang bi chiem, dang giai phong...
    taskkill /PID %%a /F >nul 2>&1
)

start cmd /k "npm start"
cd ..

echo.
echo [2/2] Dang khoi dong Game...
if exist "Build\OngChuMMO_Live.exe" (
    start "" "Build\OngChuMMO_Live.exe"
    echo Thanh cong!
) else if exist "Build\TIKTOK_LIVE_BAR.exe" (
    start "" "Build\TIKTOK_LIVE_BAR.exe"
    echo Thanh cong! (dang dung file build cu)
) else (
    echo.
    echo [Luu y] Khong tim thay file Game trong thu muc Build.
    echo Neu ban clone tu GitHub, ban can build Unity truoc:
    echo   1. Mo Unity Hub
    echo   2. Add project "UnityProject" vao
    echo   3. Build ra thu muc Build
    echo.
    echo Hoac chay file build.bat neu da cai Unity.
    echo Node Bridge van chay binh thuong, ban co the mo Control Panel:
    echo   http://127.0.0.1:3000/control.html
)
echo.
pause
