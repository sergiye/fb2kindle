@ECHO OFF
set fb2folder="books"
::set fb2folder="%~1"
::set fb2folder=%CD%

::�����������
for /R "%fb2folder%\" %%a in ("*.fb2.zip") do "%~dp07za.exe" e "%%~a" -y -o"%%~dpa"
::������� ������
for /R "%fb2folder%\" %%a in ("*.fb2.zip") do del /f /q "%%~a"

::��� ������ �� ������
GOTO Kindle
::SET /P mode=Enter 'a' if you start it from Kindle: 
::IF "%mode%"=="a" GOTO Kindle
::���������������
for /R "%fb2folder%\" %%a in ("*.fb2") do "%~dp0Fb2Kindle.exe" "%%~a" -css styles.css -d -nh
GOTO End
:Kindle
::���������������
for /R "%fb2folder%\" %%a in ("*.fb2") do "%~dp0Fb2Kindle.exe" "%%~a" -d -nh
move /Y "%fb2folder%\*.mobi" ..\documents
:End

::���������
::for /R "%fb2folder%\" %%a in ("*.fb2") do "%~dp07za.exe" a -tzip -y -o"%%~dpa" -mx9 "%%~a.zip" "%%~a"
:: ������� fb2 �����
::for /R "%fb2folder%\" %%a in ("*.fb2") do del /f /q "%%~a"
::pause