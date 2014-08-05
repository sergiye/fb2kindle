::@ECHO OFF
if "%~x1"==".fb2" goto single
:archive
	set workDir=%temp%\%~n1
	::set workDir="%~dp1%~n1"
	::del %temp%*.mobi -y
	mkdir "%workDir%"
	7za.exe e "%~1" -y -o"%workDir%"
	::Fb2Kindle.exe -a -r -j -d -c -ni
	Fb2Kindle.exe "%workDir%\*.fb2" -d -ni -c
	move /Y "%workDir%\*.mobi" "%~dp1"
	rd /s/q "%workDir%" 
goto end
::goto:eof
:single
	Fb2Kindle.exe "%~1" -d -ni -c 
goto end
:end
pause