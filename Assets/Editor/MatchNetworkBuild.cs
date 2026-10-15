#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace BrainrotPlanet.Editor
{
    public static class MatchNetworkBuild
    {
        public static void Server() => Build(BuildTarget.StandaloneLinux64, "Builds/ServerMatchmaker/BrainrotMatchServer.x86_64", true);
        public static void Windows() => Build(BuildTarget.StandaloneWindows64, "Builds/NetworkWindows/BrainrotMatch.exe", false);
        public static void Android()
        {
            // Use the standard Java Activity: the native GameActivity entry froze
            // before scene Awake on repeated RMX3151 Android 13 cold starts.
            PlayerSettings.Android.applicationEntry=AndroidApplicationEntry.Activity;
            PlayerSettings.bundleVersion="0.7.25-player-profile-test";
            PlayerSettings.Android.bundleVersionCode=69;
            Build(BuildTarget.Android,"Builds/Android/BrainrotPlanet-v0.7.25-player-profile-test.apk",false);
        }
        private static void Build(BuildTarget target,string output,bool server)
        {
            MatchProtocolSmoke.Run();
            PlayerRecordsSmoke.Run();
            AiBalanceSmoke.Run();
            SkinCatalogSmoke.Run();
            StoreSmoke.Run();
            HeroMenuSmoke.Run();
            GameplayFeedbackSmoke.Run();
            TralaleroShoeSmoke.Run();
            PlanetBiomeSmoke.Run();
            PlanetTerrainSmoke.Run();
            PlanetPaintEdgeSmoke.Run();
            PlanetPolePaintSmoke.Run();
            CapturedPaintStrokeSmoke.Run();
            DeploymentFlightSmoke.Run();
            BiomeMovementSmoke.Run();
            CastSkinSmoke.Run();
            TerritoryJoinSmoke.Run(); MatchOutcomeSmoke.Run(); ActorCollisionSmoke.Run();
            RuntimeShaderAssets.Ensure();
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            if(target!=BuildTarget.Android)
                PlayerSettings.SetScriptingBackend(server?NamedBuildTarget.Server:NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[]{"Assets/Scenes/PlanetArena.unity"}, locationPathName=output, target=target,
                subtarget=server?(int)StandaloneBuildSubtarget.Server:(int)StandaloneBuildSubtarget.Player,
                options=server?BuildOptions.None:BuildOptions.Development
            });
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Network build failed: "+report.summary.result);
            Debug.Log("MATCH_BUILD_PASS target="+target+" server="+server+" output="+output);
        }
    }
}
#endif
