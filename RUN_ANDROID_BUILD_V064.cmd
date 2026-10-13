@echo off
setlocal
set "ROOT=C:\Rabota\projects\BrainrotPlanetUnity"
set "EDITOR=C:\Program Files\Unity\Hub\Editor\6000.2.7f2\Editor\Unity.exe"
"%EDITOR%" -batchmode -quit -projectPath "%ROOT%" -executeMethod BrainrotPlanet.Editor.PlanetEditorSetup.BuildAndroid -logFile "%ROOT%\unity-build-v064.log"
exit /b %ERRORLEVEL%
