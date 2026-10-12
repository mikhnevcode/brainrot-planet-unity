#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.Build;

namespace BrainrotPlanet.Editor
{
    [InitializeOnLoad]
    public static class PlanetEditorSetup
    {
        private const string Arena = "Assets/Scenes/PlanetArena.unity";
        static PlanetEditorSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)return;
                if(!File.Exists(Arena))CreateScene(true);
            };
        }

        [MenuItem("Brainrot Planet/1. Create and open game scene")]
        public static void SetupMenu()=>CreateScene(true);

        private static void CreateScene(bool open)
        {
            if(!Directory.Exists("Assets/Scenes"))Directory.CreateDirectory("Assets/Scenes");
            if(!File.Exists(Arena))
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var main=new GameObject("Brainrot Planet: Game Director");
                main.AddComponent<PlanetGame>();
                EditorSceneManager.SaveScene(scene,Arena);
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Arena,true)};
            if(open)EditorSceneManager.OpenScene(Arena);
            AssetDatabase.Refresh();
        }

        [MenuItem("Brainrot Planet/2. Build Android APK")]
        public static void BuildAndroid()
        {
            TerritoryJoinSmoke.Run();
            RuntimeShaderAssets.Ensure();
            TralaleroRigSetup.Prepare();
            BombardiroRigSetup.Prepare();
            BombombiniRigSetup.Prepare();
            BallerinaRigSetup.Prepare();
            RemainingHeroesRigSetup.Prepare();
            CreateScene(false);
            PlayerSettings.companyName="mikhnevcode";
            PlayerSettings.productName="Brainrot Planet IO";
            PlayerSettings.bundleVersion="0.4.3";
            PlayerSettings.Android.bundleVersionCode=25;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.mikhnevcode.brainrotplanet");
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft=false;
            PlayerSettings.allowedAutorotateToLandscapeRight=false;
            PlayerSettings.allowedAutorotateToPortrait=true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            // Primitive colliders are instantiated dynamically at runtime.
            PlayerSettings.stripEngineCode=false;
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevelAuto;
            // Prefer GLES3: Mali Vulkan devices can stall during repeated GameActivity startup.
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new [] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android,BuildTarget.Android);
            var dir=Path.Combine(Directory.GetCurrentDirectory(),"Builds","Android");
            Directory.CreateDirectory(dir);
            var output=Path.Combine(dir,"BrainrotPlanet-v0.4.3.apk");
            var opts=new BuildPlayerOptions{
                scenes=new[]{Arena},locationPathName=output,
                target=BuildTarget.Android,options=BuildOptions.Development
            };
            BuildReport result=BuildPipeline.BuildPlayer(opts);
            if(result.summary.result!=BuildResult.Succeeded)
                throw new Exception("Android build failed. See Editor.log and Build Report. Status: "+result.summary.result);
            Debug.Log("BRAINROT_PLANET_APK="+output);
        }
    }
}
#endif
