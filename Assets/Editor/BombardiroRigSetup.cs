#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BrainrotPlanet.Editor
{
    public static class BombardiroRigSetup
    {
        private const string Fbx = "Assets/Resources/Characters/Bombardiro_v06.fbx";
        private const string Controller = "Assets/Resources/Characters/Bombardiro.controller";

        [MenuItem("Brainrot Planet/3. Import animated Bombardiro")]
        public static void Prepare()
        {
            PrepareSkinTexture("Assets/Resources/Characters/Bombardiro_v06_Skin_Albedo.png",false);
            PrepareSkinTexture("Assets/Resources/Characters/Bombardiro_v06_Skin_Normal.png",true);
            PrepareSkinMaterial();
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
            if(model==null)
            {
                Debug.LogWarning("BOMBARDIRO_NOT_FOUND "+Fbx);
                return;
            }
            ModelImporter importer=AssetImporter.GetAtPath(Fbx) as ModelImporter;
            if(importer==null)
                throw new Exception("Bombardiro FBX does not have a ModelImporter");
            if(!importer.importAnimation || importer.animationType!=ModelImporterAnimationType.Generic)
            {
                importer.importAnimation=true;
                importer.animationType=ModelImporterAnimationType.Generic;
                importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
            }
            var clips=new List<AnimationClip>();
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(Fbx))
            {
                if(asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    clips.Add(clip);
                    Debug.Log("BOMBARDIRO_IMPORTED_CLIP="+clip.name+" LENGTH="+clip.length.ToString("F2"));
                }
            }
            if(clips.Count==0)
            {
                Debug.LogError("BOMBARDIRO_NO_IMPORTED_ANIMATION_CLIPS");
                return;
            }
            AnimatorController controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
            if(controller!=null)
                AssetDatabase.DeleteAsset(Controller);
            controller=AnimatorController.CreateAnimatorControllerAtPath(Controller);
            controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded",AnimatorControllerParameterType.Bool);
            AnimatorStateMachine sm=controller.layers[0].stateMachine;
            var idle=sm.AddState("Idle");
            var run=sm.AddState("Run");
            var jump=sm.AddState("Jump");
            var land=sm.AddState("Land");
            sm.defaultState=idle;
            idle.motion=Find(clips,"Idle") ?? clips[0];
            run.motion=Find(clips,"Run") ?? idle.motion;
            jump.motion=Find(clips,"Jump") ?? idle.motion;
            land.motion=Find(clips,"Land") ?? idle.motion;
            // Configure deterministic transitions for start, run, jump and landing.
            var tr=idle.AddTransition(run);tr.hasExitTime=false;tr.duration=.12f;
            tr.AddCondition(AnimatorConditionMode.Greater,.15f,"Speed");
            tr=run.AddTransition(idle);tr.hasExitTime=false;tr.duration=.15f;
            tr.AddCondition(AnimatorConditionMode.Less,.15f,"Speed");
            foreach(var s in new[]{idle,run})
            {
                tr=s.AddTransition(jump);tr.hasExitTime=false;tr.duration=.06f;
                tr.AddCondition(AnimatorConditionMode.IfNot,0,"Grounded");
            }
            tr=jump.AddTransition(land);tr.hasExitTime=false;tr.duration=.10f;
            tr.AddCondition(AnimatorConditionMode.If,0,"Grounded");
            tr=land.AddTransition(run);tr.hasExitTime=true;tr.exitTime=.85f;tr.duration=.1f;
            tr.AddCondition(AnimatorConditionMode.Greater,.15f,"Speed");
            tr=land.AddTransition(idle);tr.hasExitTime=true;tr.exitTime=.85f;tr.duration=.1f;
            tr.AddCondition(AnimatorConditionMode.Less,.15f,"Speed");
            AssetDatabase.SaveAssets();
            Debug.Log("BOMBARDIRO_ANIMATOR_READY clips="+clips.Count+" controller="+Controller);
        }

        private static void PrepareSkinTexture(string path,bool normal)
        {
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new Exception("Bombardiro skin texture missing: "+path);
            var type=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            if(importer.textureType==type&&importer.sRGBTexture==!normal&&
                importer.maxTextureSize==1024&&importer.mipmapEnabled)return;
            importer.textureType=type;importer.sRGBTexture=!normal;
            importer.maxTextureSize=1024;importer.mipmapEnabled=true;
            importer.wrapMode=TextureWrapMode.Repeat;
            importer.SaveAndReimport();
        }

        private static void PrepareSkinMaterial()
        {
            const string path="Assets/Resources/Characters/Bombardiro_v06_Skin.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null)
            {
                var template=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/PlanetStandard.mat");
                if(template==null)throw new Exception("PlanetStandard material is missing");
                material=new Material(template){name="Bombardiro v06 Skin"};
                AssetDatabase.CreateAsset(material,path);
            }
            // A Resources material preserves the Standard normal-map shader
            // variant when Android strips unused variants at build time.
            material.color=Color.white;
            material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Resources/Characters/Bombardiro_v06_Skin_Albedo.png");
            material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Resources/Characters/Bombardiro_v06_Skin_Normal.png"));
            material.SetFloat("_BumpScale",1f);material.SetFloat("_Mode",0f);
            material.SetFloat("_Glossiness",.12f);
            material.SetFloat("_SpecularHighlights",0f);
            material.SetFloat("_GlossyReflections",0f);
            material.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();
        }

        private static AnimationClip Find(List<AnimationClip> clips,string partial)
        {
            foreach(var clip in clips)
                if(clip.name.IndexOf(partial,StringComparison.OrdinalIgnoreCase)>=0)
                    return clip;
            return null;
        }
    }
}
#endif
