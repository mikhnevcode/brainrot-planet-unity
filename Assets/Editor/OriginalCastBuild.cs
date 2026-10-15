#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace BrainrotPlanet.Editor
{
    public static class OriginalCastBuild
    {
        public static void Android()=>Build(BuildTarget.Android,"Builds/Android/BrainrotPlanet-v0.8.0-original26-test.apk",false);
        public static void AndroidRollo()=>Build(BuildTarget.Android,"Builds/Android/BrainrotPlanet-v0.8.1-rollo-detail-test.apk",false,"0.8.1-rollo-detail-test",71);
        public static void Windows()=>Build(BuildTarget.StandaloneWindows64,"Builds/Original26Windows/BrainrotPlanet.exe",false);
        public static void Server()=>Build(BuildTarget.StandaloneLinux64,"Builds/Original26Server/BrainrotMatchServer.x86_64",true);
        // Preserve the old assets/GUIDs for history, but do not ship the retired
        // characters through Resources in a build of the new original cast.
        public static void ArchiveLegacy()
        {
            if(!AssetDatabase.IsValidFolder("Assets/LegacyCharacterArchive"))AssetDatabase.CreateFolder("Assets","LegacyCharacterArchive");
            Move("Assets/Resources/Characters","Assets/LegacyCharacterArchive/Characters");
            Move("Assets/Resources/HeroPortraits064.png","Assets/LegacyCharacterArchive/HeroPortraits064.png");
            AssetDatabase.SaveAssets();
        }
        private static void Move(string source,string target)
        {
            if(AssetDatabase.LoadMainAssetAtPath(source)==null)return;
            if(AssetDatabase.LoadMainAssetAtPath(target)!=null)throw new Exception("Archive target already exists: "+target);
            string error=AssetDatabase.MoveAsset(source,target);if(!string.IsNullOrEmpty(error))throw new Exception(error);
        }
        public static void Regression()
        {
            OriginalCastSetup.Smoke();RolloDetailSetup.Smoke();HeroMenuSmoke.Run();StoreSmoke.Run();
            MatchProtocolSmoke.Run();PlayerRecordsSmoke.Run();AiBalanceSmoke.Run();
            GameplayFeedbackSmoke.Run();ExplosionAudioSmoke.Run();
            PlanetBiomeSmoke.Run();PlanetTerrainSmoke.Run();PlanetPaintEdgeSmoke.Run();
            PlanetPolePaintSmoke.Run();CapturedPaintStrokeSmoke.Run();DeploymentFlightSmoke.Run();
            BiomeMovementSmoke.Run();TerritoryJoinSmoke.Run();MatchOutcomeSmoke.Run();ActorCollisionSmoke.Run();
            Debug.Log("ORIGINAL26_REGRESSION_PASS protocol="+MatchProtocol.Version+" heroes="+OriginalHeroCatalog.Count);
        }
        private static void Build(BuildTarget target,string output,bool server,string version="0.8.0-original26-test",int code=70)
        {
            ArchiveLegacy();Regression();RuntimeShaderAssets.Ensure();
            PlayerSettings.bundleVersion=version;
            PlayerSettings.Android.bundleVersionCode=code;
            if(target==BuildTarget.Android)PlayerSettings.Android.applicationEntry=AndroidApplicationEntry.Activity;
            else PlayerSettings.SetScriptingBackend(server?NamedBuildTarget.Server:NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[]{"Assets/Scenes/PlanetArena.unity"},locationPathName=output,target=target,
                subtarget=server?(int)StandaloneBuildSubtarget.Server:(int)StandaloneBuildSubtarget.Player,
                options=server?BuildOptions.None:BuildOptions.Development
            });
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Original cast build failed: "+report.summary.result);
            Debug.Log("ORIGINAL26_BUILD_PASS target="+target+" server="+server+" bytes="+report.summary.totalSize+" output="+output);
        }
    }
}
#endif
