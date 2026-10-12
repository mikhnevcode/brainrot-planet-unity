#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BrainrotPlanet.Editor
{
    /// <summary>Common rig and Animator setup for the remaining eight first-pass characters.</summary>
    public static class RemainingHeroesRigSetup
    {
        public static readonly string[] Names = {
            "Assassino","Patapim","TungSahur","Lirili",
            "Frigo","Frulli","Boneca","Trippi"
        };

        [MenuItem("Brainrot Planet/5. Import remaining character models")]
        public static void Prepare()
        {
            foreach (string name in Names) PrepareOne(name);
            AssetDatabase.SaveAssets();
        }

        private static void PrepareOne(string name)
        {
            string path = "Assets/Resources/Characters/"+name+"_v02.fbx";
            string controllerPath = "Assets/Resources/Characters/"+name+".controller";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(prefab == null) throw new Exception("Missing hero FBX: "+path);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if(importer == null) throw new Exception("Missing ModelImporter: "+path);
            if(!importer.importAnimation || importer.animationType != ModelImporterAnimationType.Generic)
            {
                importer.importAnimation=true;
                importer.animationType=ModelImporterAnimationType.Generic;
                importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
            }
            var clips = new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if(!(asset is AnimationClip clip) || clip.name.StartsWith("__preview__")) continue;
                foreach(var action in new[]{"Idle","Run","Jump","Land","Victory"})
                    if(clip.name.IndexOf("|"+action, StringComparison.OrdinalIgnoreCase)>=0 ||
                        string.Equals(clip.name,action,StringComparison.OrdinalIgnoreCase))
                        clips[action]=clip;
            }
            foreach(var action in new[]{"Idle","Run","Jump","Land","Victory"})
                if(!clips.ContainsKey(action))
                    throw new Exception("Missing "+action+" clip for "+name);
            var old=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if(old!=null) AssetDatabase.DeleteAsset(controllerPath);
            var ctrl=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            ctrl.AddParameter("Speed",AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Grounded",AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Victory",AnimatorControllerParameterType.Trigger);
            var sm=ctrl.layers[0].stateMachine;
            var idle=sm.AddState("Idle");idle.motion=clips["Idle"];sm.defaultState=idle;
            var run=sm.AddState("Run");run.motion=clips["Run"];
            var jump=sm.AddState("Jump");jump.motion=clips["Jump"];
            var land=sm.AddState("Land");land.motion=clips["Land"];
            var victory=sm.AddState("Victory");victory.motion=clips["Victory"];
            AnimatorStateTransition tr=idle.AddTransition(run);
            tr.hasExitTime=false;tr.duration=.11f;
            tr.AddCondition(AnimatorConditionMode.Greater,.15f,"Speed");
            tr=run.AddTransition(idle);tr.hasExitTime=false;tr.duration=.13f;
            tr.AddCondition(AnimatorConditionMode.Less,.15f,"Speed");
            foreach(var s in new[]{idle,run})
            {
                tr=s.AddTransition(jump);tr.hasExitTime=false;tr.duration=.05f;
                tr.AddCondition(AnimatorConditionMode.IfNot,0,"Grounded");
            }
            tr=jump.AddTransition(land);tr.hasExitTime=false;tr.duration=.09f;
            tr.AddCondition(AnimatorConditionMode.If,0,"Grounded");
            tr=land.AddTransition(run);tr.hasExitTime=true;tr.exitTime=.85f;tr.duration=.1f;
            tr.AddCondition(AnimatorConditionMode.Greater,.15f,"Speed");
            tr=land.AddTransition(idle);tr.hasExitTime=true;tr.exitTime=.85f;tr.duration=.1f;
            tr.AddCondition(AnimatorConditionMode.Less,.15f,"Speed");
            tr=sm.AddAnyStateTransition(victory);tr.hasExitTime=false;tr.duration=.09f;
            tr.AddCondition(AnimatorConditionMode.If,0,"Victory");
            tr=victory.AddTransition(idle);tr.hasExitTime=true;tr.exitTime=.98f;tr.duration=.12f;
            Debug.Log("REMAINING_HERO_READY "+name+" clips="+clips.Count);
        }
    }
}
#endif
