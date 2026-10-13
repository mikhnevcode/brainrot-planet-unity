#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BrainrotPlanet.Editor
{
    /// <summary>
    /// Attach authored Drop, Special and VictoryShow clips to the animator for
    /// every hero, without replacing existing Idle/Run/Jump/Land state logic.
    /// </summary>
    public static class HeroAnimationUpgrade
    {
        private struct Item
        {
            public string Name, Version;
            public Item(string name,string version){Name=name;Version=version;}
        }

        private static readonly Item[] Heroes =
        {
            new Item("Tralalero","v04"),
            new Item("Bombardiro","v06"),
            new Item("Frigo","v03"),
            new Item("Bombombini","v03"),
            new Item("Ballerina","v03"),
            new Item("Assassino","v03"),
            new Item("Patapim","v03"),
            new Item("TungSahur","v03"),
            new Item("Lirili","v03"),
            new Item("Frulli","v03"),
            new Item("Boneca","v03"),
            new Item("Trippi","v03")
        };

        [MenuItem("Brainrot Planet/7. Import authored character animations")]
        public static void Prepare()
        {
            foreach(Item item in Heroes)SetupOne(item);
            AssetDatabase.SaveAssets();
            Debug.Log("HERO_ANIMATION_UPGRADE_READY count="+Heroes.Length);
        }

        private static AnimationClip FindClip(string path,string action)
        {
            AnimationClip result=null;
            foreach(UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                var clip=asset as AnimationClip;
                if(clip==null || clip.name.StartsWith("__preview__"))continue;
                if(clip.name.Equals(action,StringComparison.OrdinalIgnoreCase) ||
                   clip.name.EndsWith("|"+action,StringComparison.OrdinalIgnoreCase))
                {
                    if(result!=null)
                        throw new Exception("Duplicate '"+action+"' clip in "+path);
                    result=clip;
                }
            }
            if(result==null)
                throw new Exception("Expected '"+action+"' animation in "+path);
            return result;
        }

        private static AnimatorState Ensure(AnimatorStateMachine machine,string name)
        {
            foreach(var item in machine.states)
                if(item.state.name==name)return item.state;
            return machine.AddState(name);
        }

        private static void Param(AnimatorController controller,string name,
                                  AnimatorControllerParameterType type)
        {
            foreach(var p in controller.parameters)
            {
                if(p.name!=name)continue;
                if(p.type!=type)
                    throw new Exception("Bad parameter "+name+" "+p.type+" expected "+type);
                return;
            }
            controller.AddParameter(name,type);
        }

        private static bool HasAnyTrigger(AnimatorStateMachine machine,string parameter)
        {
            foreach(var tr in machine.anyStateTransitions)
                foreach(var c in tr.conditions)
                    if(c.parameter==parameter)return true;
            return false;
        }

        private static void SetupOne(Item item)
        {
            string file="Assets/Resources/Characters/"+item.Name+"_"+item.Version+".fbx";
            string controllerPath="Assets/Resources/Characters/"+item.Name+".controller";
            if(AssetDatabase.LoadAssetAtPath<GameObject>(file)==null)
                throw new Exception("Missing new character FBX "+file);
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if(controller==null)
                throw new Exception("Missing character AnimatorController "+controllerPath);

            AnimationClip drop=FindClip(file,"Drop");
            AnimationClip special=FindClip(file,"Special");
            AnimationClip victoryClip=FindClip(file,"VictoryShow");

            Param(controller,"Dropping",AnimatorControllerParameterType.Bool);
            Param(controller,"Special",AnimatorControllerParameterType.Trigger);
            Param(controller,"Victory",AnimatorControllerParameterType.Trigger);

            var sm=controller.layers[0].stateMachine;
            var idle=Ensure(sm,"Idle");
            var land=Ensure(sm,"Land");
            var victory=Ensure(sm,"Victory");
            var dropState=Ensure(sm,"Drop");
            var specialState=Ensure(sm,"Special");
            dropState.motion=drop;
            specialState.motion=special;
            victory.motion=victoryClip;

            var dropStart=sm.AddAnyStateTransition(dropState);
            dropStart.hasExitTime=false;
            dropStart.duration=.04f;
            dropStart.canTransitionToSelf=false;
            dropStart.AddCondition(AnimatorConditionMode.If,0,"Dropping");

            var dropEnd=dropState.AddTransition(land);
            dropEnd.hasExitTime=false;
            dropEnd.duration=.09f;
            dropEnd.AddCondition(AnimatorConditionMode.IfNot,0,"Dropping");

            var attack=sm.AddAnyStateTransition(specialState);
            attack.hasExitTime=false;
            attack.duration=.07f;
            attack.canTransitionToSelf=false;
            attack.AddCondition(AnimatorConditionMode.If,0,"Special");
            attack.AddCondition(AnimatorConditionMode.IfNot,0,"Dropping");

            var specialEnd=specialState.AddTransition(idle);
            specialEnd.hasExitTime=true;
            specialEnd.exitTime=.91f;
            specialEnd.duration=.12f;

            if(!HasAnyTrigger(sm,"Victory"))
            {
                var win=sm.AddAnyStateTransition(victory);
                win.hasExitTime=false;
                win.duration=.09f;
                win.canTransitionToSelf=false;
                win.AddCondition(AnimatorConditionMode.If,0,"Victory");

                var recovery=victory.AddTransition(idle);
                recovery.hasExitTime=true;
                recovery.exitTime=.98f;
                recovery.duration=.13f;
            }

            EditorUtility.SetDirty(controller);
            Debug.Log("HERO_AUTHORED_CLIPS_OK hero="+item.Name+
                " drop="+drop.name+" special="+special.name+
                " victory="+victoryClip.name);
        }
    }
}
#endif
