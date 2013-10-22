@ECHO OFF
set fb2folder="."

for /R "%fb2folder%\" %%a in ("*.fb2.zip") do "%~dp07za.exe" e "%%~a" -y -o"%%~dpa"
for /R "%fb2folder%\" %%a in ("*.fb2.zip") do del /f /q "%%~a"

for /R "%fb2folder%\" %%a in ("*.fb2") do "%~dp0fb2mobi.exe" "%%~a" -us -nt -nc -cl

pause