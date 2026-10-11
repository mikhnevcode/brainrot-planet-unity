@echo off
setlocal
set "PROJECT=%~dp0"
set "UNITY=C:\Program Files\Unity\Hub\Editor\6000.2.7f2\Editor\Unity.exe"
echo Brainrot Planet IO / Unity Android build
if not exist "%UNITY%" (
  echo ERROR: Unity 6000.2.7f2 not found at %UNITY%
  echo Install the matching editor through Unity Hub.
  pause
  exit /b 2
)
"%UNITY%" -batchmode -nographics -quit -projectPath "%PROJECT%" -executeMethod BrainrotPlanet.Editor.PlanetEditorSetup.BuildAndroid -logFile "%PROJECT%unity-build.log"
if errorlevel 1 (
  echo BUILD FAILED. See unity-build.log for details.
  pause
  exit /b 1
)
if not exist "%PROJECT%Builds\Android\BrainrotPlanet-v0.1.apk" (
  echo Build returned but APK is not present. Check unity-build.log.
  pause
  exit /b 3
)
echo SUCCESS: %PROJECT%Builds\Android\BrainrotPlanet-v0.1.apk
pause
