@echo off
echo =======================================
echo     KHOI DONG ONG CHU MMO LIVE
echo =======================================

echo.
echo [1/2] Dang khoi dong Node Bridge...
cd TikTokBridge
start cmd /k "npm start"
cd ..

echo.
echo [2/2] Dang khoi dong Game...
if exist "Build\OngChuMMO_Live.exe" (
    start "" "Build\OngChuMMO_Live.exe"
    echo Thanh cong!
) else (
    echo Khong tim thay file Build\OngChuMMO_Live.exe!
    echo Vui long chay build.bat truoc de tao file chay Game.
)
echo.
pause
