#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BrainrotPlanet.Editor
{
    public static class TralaleroRigSetup
    {
        private const string Fbx = "Assets/Resources/Characters/Tralalero_v06.fbx";
        private const string Controller = "Assets/Resources/Characters/Tralalero.controller";

        [MenuItem("Brainrot Planet/3. Import animated Tralalero")]
        public static void Prepare()
        {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
            if(model==null)
            {
                Debug.LogWarning("TRALALERO_NOT_FOUND "+Fbx);
                return;
            }
            ModelImporter importer=AssetImporter.GetAtPath(Fbx) as ModelImporter;
            if(importer==null)
                throw new Exception("Tralalero FBX does not have a ModelImporter");
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
                    Debug.Log("TRALALERO_IMPORTED_CLIP="+clip.name+" LENGTH="+clip.length.ToString("F2"));
                }
            }
            if(clips.Count==0)
            {
                Debug.LogError("TRALALERO_NO_IMPORTED_ANIMATION_CLIPS");
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
            Debug.Log("TRALALERO_ANIMATOR_READY clips="+clips.Count+" controller="+Controller);
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
