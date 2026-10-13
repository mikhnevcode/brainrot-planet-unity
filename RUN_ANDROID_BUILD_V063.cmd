@echo off
setlocal
set "ROOT=C:\Rabota\projects\BrainrotPlanetUnity"
set "EDITOR=C:\Program Files\Unity\Hub\Editor\6000.2.7f2\Editor\Unity.exe"
echo STARTED %DATE% %TIME% > "%ROOT%\interactive-build-v063-status.txt"
"%EDITOR%" -batchmode -quit -projectPath "%ROOT%" -executeMethod BrainrotPlanet.Editor.PlanetEditorSetup.BuildAndroid -logFile "%ROOT%\unity-interactive-v063.log"
set "RESULT=%ERRORLEVEL%"
echo EXITCODE=%RESULT% %DATE% %TIME% > "%ROOT%\interactive-build-v063-status.txt"
exit /b %RESULT%
