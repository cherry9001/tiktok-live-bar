@echo off
echo =======================================
echo     BUILD GAME ONG CHU MMO LIVE
echo =======================================
echo.
echo Dang build file exe... Qua trinh nay co the mat vai phut, vui long doi.
echo.

"C:\Program Files\Unity\Hub\Editor\6000.2.10f1\Editor\Unity.exe" -quit -batchmode -projectPath "%~dp0UnityProject" -buildWindows64Player "%~dp0Build\OngChuMMO_Live.exe" -logFile "%~dp0build_log.txt"

echo.
echo Da build xong! File chay duoc luu tai thu muc Build.
echo Ban co the chay file run.bat de bat dau.
echo.
pause
