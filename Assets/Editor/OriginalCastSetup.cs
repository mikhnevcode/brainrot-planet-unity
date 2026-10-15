#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace BrainrotPlanet.Editor
{
    public static class OriginalCastSetup
    {
        private static readonly string[] Actions={"Idle","Run","Jump","Land","Victory","Drop","Special","VictoryShow"};
        public static void PrepareAndRender()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var hero in OriginalHeroCatalog.All)Prepare(hero);
            AssetDatabase.SaveAssets();
            Smoke();Portraits();
            Debug.Log("ORIGINAL_CAST_PREPARE_PASS heroes=26 animations=208 skins=130 portraits=26");
        }
        private static string ActionName(string value)
        {int split=value.LastIndexOf('|');return split<0?value:value.Substring(split+1);}
        private static void Prepare(OriginalHeroCatalog.Hero hero)
        {
            int heroIndex=Array.IndexOf(OriginalHeroCatalog.All,hero);
            string path="Assets/Resources/"+OriginalHeroCatalog.Resource(heroIndex)+".fbx";
            var importer=AssetImporter.GetAtPath(path) as ModelImporter;
            if(importer==null)throw new Exception("Missing original model "+path);
            importer.animationType=ModelImporterAnimationType.Generic;
            importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation=true;importer.animationCompression=ModelImporterAnimationCompression.Off;
            importer.importBlendShapes=false;importer.importCameras=false;importer.importLights=false;
            importer.isReadable=true;
            var settings=importer.defaultClipAnimations;
            foreach(var clip in settings)
            {
                clip.name=ActionName(clip.name);
                clip.loopTime=clip.name=="Idle"||clip.name=="Run"||clip.name=="Drop"||clip.name=="VictoryShow";
                clip.lockRootRotation=true;clip.lockRootPositionXZ=true;clip.lockRootHeightY=true;
            }
            importer.clipAnimations=settings;importer.SaveAndReimport();
            var clips=new Dictionary<string,AnimationClip>();
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if(asset is AnimationClip clip&&!clip.name.StartsWith("__preview"))clips[ActionName(clip.name)]=clip;
            foreach(string action in Actions)if(!clips.ContainsKey(action))throw new Exception("Original clip missing "+hero.Id+"/"+action);
            string controllerPath="Assets/Resources/OriginalCast/"+hero.Id+".controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            else
            {
                var layer=controller.layers[0];
                foreach(var transition in layer.stateMachine.anyStateTransitions)layer.stateMachine.RemoveAnyStateTransition(transition);
                foreach(var state in layer.stateMachine.states)layer.stateMachine.RemoveState(state.state);
                controller.parameters=new AnimatorControllerParameter[0];
            }
            controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded",AnimatorControllerParameterType.Bool);
            controller.AddParameter("Dropping",AnimatorControllerParameterType.Bool);
            controller.AddParameter("Special",AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Victory",AnimatorControllerParameterType.Trigger);
            var sm=controller.layers[0].stateMachine;var states=new Dictionary<string,AnimatorState>();
            foreach(string action in Actions){var state=sm.AddState(action);state.motion=clips[action];states[action]=state;}
            sm.defaultState=states["Idle"];
            Transition(states["Idle"],states["Run"],"Speed",AnimatorConditionMode.Greater,.15f);
            Transition(states["Run"],states["Idle"],"Speed",AnimatorConditionMode.Less,.15f);
            foreach(var name in new[]{"Idle","Run"})
            {
                var jump=Transition(states[name],states["Jump"],"Grounded",AnimatorConditionMode.IfNot);
                jump.AddCondition(AnimatorConditionMode.IfNot,0,"Dropping");
            }
            Transition(states["Jump"],states["Land"],"Grounded",AnimatorConditionMode.If);
            var land=states["Land"].AddTransition(states["Idle"]);land.hasExitTime=true;land.exitTime=.94f;land.duration=.07f;
            var drop=sm.AddAnyStateTransition(states["Drop"]);drop.hasExitTime=false;drop.duration=.05f;drop.canTransitionToSelf=false;drop.AddCondition(AnimatorConditionMode.If,0,"Dropping");
            Transition(states["Drop"],states["Idle"],"Dropping",AnimatorConditionMode.IfNot);
            foreach(var action in new[]{"Special","Victory"})
            {
                var tr=sm.AddAnyStateTransition(states[action]);tr.hasExitTime=false;tr.duration=.07f;tr.canTransitionToSelf=false;tr.AddCondition(AnimatorConditionMode.If,0,action);
                var end=states[action].AddTransition(states["Idle"]);end.hasExitTime=true;end.exitTime=.98f;end.duration=.08f;
            }
            EditorUtility.SetDirty(controller);
            Debug.Log("ORIGINAL_CONTROLLER_READY id="+hero.Id+" clips="+clips.Count);
        }
        private static AnimatorStateTransition Transition(AnimatorState from,AnimatorState to,string parameter,AnimatorConditionMode mode,float threshold=0)
        {var tr=from.AddTransition(to);tr.hasExitTime=false;tr.duration=.08f;tr.AddCondition(mode,threshold,parameter);return tr;}
        public static void Smoke()
        {
            var manifest=SkinCatalog.ReadManifest(File.ReadAllText("Content/OriginalCast/Skins/"+SkinCatalog.CatalogFile));
            if(manifest.skins.Length!=130||OriginalHeroCatalog.Count!=26)throw new Exception("Original roster/catalog count");
            int skins=0,clips=0;
            for(int hero=0;hero<OriginalHeroCatalog.Count;hero++)
            {
                var root=new GameObject("Original smoke "+hero);
                try
                {
                    AvatarArt.Make(root.transform,hero,Color.cyan);
                    var visual=SkinRigProfile.Visual(root.transform,hero);if(visual==null)throw new Exception("Original visual missing "+hero);
                    var animator=visual.GetComponent<Animator>();var animationClips=animator.runtimeAnimatorController.animationClips;
                    foreach(string action in Actions)
                    {
                        AnimationClip found=null;foreach(var clip in animationClips)if(ActionName(clip.name)==action)found=clip;
                        if(found==null)throw new Exception("Clip not attached "+hero+" "+action);
                        found.SampleAnimation(visual.gameObject,found.length*.39f);clips++;
                    }
                    HeroPreviewFraming.SampleIdle(root.transform);
                    var bounds=AvatarArt.PoseBounds(root.transform);
                    if(bounds.size.y<.5f||bounds.size.y>1.5f)throw new Exception("Original scale "+hero+" "+bounds.size);
                    foreach(var renderer in root.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials)
                        if(material.color.a<.999f||material.renderQueue>=3000)throw new Exception("Transparent original body "+hero);
                    int count=0;
                    foreach(var entry in manifest.skins)if(entry.hero==hero)
                    {
                        var pack=SkinCatalog.ReadPack(File.ReadAllBytes("Content/OriginalCast/Skins/"+entry.file),entry);
                        if(pack.bindings.Length==0||pack.meshes.Length==0)throw new Exception("Empty original cosmetic "+entry.id);
                        foreach(var part in pack.meshes)if(!SkinRigProfile.AllowsAnchor(hero,part.anchor))throw new Exception("Original skin anchor "+entry.id);
                        var dressed=new GameObject("Original skin smoke "+entry.id);
                        try
                        {
                            AvatarArt.Make(dressed.transform,hero,Color.cyan);HeroPreviewFraming.SampleIdle(dressed.transform);
                            var before=AvatarArt.PoseBounds(dressed.transform);SkinVisual.Apply(dressed.transform,pack);
                            if(dressed.GetComponentInChildren<SkinVisual>()==null)throw new Exception("Skin not attached "+entry.id);
                            var after=AvatarArt.PoseBounds(dressed.transform);
                            if(after.size.magnitude>before.size.magnitude+.3f)throw new Exception("Detached skin geometry "+entry.id+" "+after.size);
                            var dressedVisual=SkinRigProfile.Visual(dressed.transform,hero);
                            foreach(var clip in dressedVisual.GetComponent<Animator>().runtimeAnimatorController.animationClips)
                                if(clip.name=="Run"||clip.name=="Drop"||clip.name=="VictoryShow")
                                {clip.SampleAnimation(dressedVisual.gameObject,clip.length*.43f);var moving=AvatarArt.PoseBounds(dressed.transform);if(moving.size.magnitude>before.size.magnitude+.7f)throw new Exception("Moving skin detached "+entry.id);}
                        }
                        finally{UnityEngine.Object.DestroyImmediate(dressed);}
                        count++;skins++;
                    }
                    if(count!=5)throw new Exception("Expected five skins for "+hero);
                }
                finally{UnityEngine.Object.DestroyImmediate(root);}
            }
            Debug.Log("ORIGINAL_CAST_SMOKE_PASS heroes=26 clips="+clips+" skins="+skins+" opaque=true");
        }
        public static void Portraits()
        {
            const int tile=256,columns=6,rows=5;
            var atlas=new Texture2D(tile*columns,tile*rows,TextureFormat.RGBA32,false);
            var target=new RenderTexture(tile,tile,24,RenderTextureFormat.ARGB32){antiAliasing=2};target.Create();
            var cameraObject=new GameObject("Original portrait camera");var camera=cameraObject.AddComponent<Camera>();
            camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.78f,.90f,.95f,1);
            camera.cullingMask=1<<30;camera.nearClipPlane=.02f;camera.farClipPlane=20;camera.allowHDR=false;
            var lightObject=new GameObject("Original portrait fill");var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.25f;light.cullingMask=1<<30;lightObject.transform.rotation=Quaternion.Euler(32,-25,0);
            for(int hero=0;hero<OriginalHeroCatalog.Count;hero++)
            {
                var root=new GameObject("Original portrait "+hero);AvatarArt.Make(root.transform,hero,Color.white);HeroPreviewFraming.SampleIdle(root.transform);
                root.transform.rotation=AvatarArt.AlignFacing(AvatarArt.LocalFacing(root.transform),Quaternion.Euler(0,28,0)*Vector3.back);
                foreach(var node in root.GetComponentsInChildren<Transform>(true))node.gameObject.layer=30;
                var bounds=AvatarArt.PoseBounds(root.transform);root.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                bounds=AvatarArt.PoseBounds(root.transform);HeroPreviewFraming.Place(camera,bounds,0,10,1.13f);camera.Render();
                RenderTexture.active=target;atlas.ReadPixels(new Rect(0,0,tile,tile),(hero%columns)*tile,(rows-1-hero/columns)*tile);RenderTexture.active=null;
                UnityEngine.Object.DestroyImmediate(root);
            }
            atlas.Apply();string path="Assets/Resources/OriginalCast/HeroPortraits080.png";File.WriteAllBytes(path,atlas.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(atlas);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(lightObject);target.Release();UnityEngine.Object.DestroyImmediate(target);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.SaveAndReimport();
            Debug.Log("ORIGINAL_PORTRAITS_READY count=26 columns=6 rows=5");
        }
    }
}
#endif
