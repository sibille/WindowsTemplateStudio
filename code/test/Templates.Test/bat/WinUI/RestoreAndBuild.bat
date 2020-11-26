REM #Setup MsBuild context by brute force, restore packages and build the solution

IF EXIST "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Preview" (
	call "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Preview\Common7\Tools\VsMSBuildCmd.bat"
)
	
ECHO ON
	
msbuild "%~1" /t:Restore;Rebuild /p:RestorePackagesPath="C:\Packs" /p:Configuration=%3;Platform=%2;RuntimeIdentifier=win10-x86
IF %ERRORLEVEL% NEQ 0 ( 
	GOTO ERROR 
)
ECHO OFF

GOTO END

:ERROR
	EXIT 1
:END


