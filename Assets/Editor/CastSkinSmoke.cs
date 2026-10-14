#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace BrainrotPlanet.Editor
{
    public static class CastSkinSmoke
    {
        public static void Run()
        {
            var manifest=SkinCatalog.ReadManifest(File.ReadAllText("Content/Skins/catalog-cast-v1.json"));
            int skins=0,poses=0;var heroCounts=new int[SkinRigProfile.HeroCount];
            foreach(var e in manifest.skins)
            {
                string path="Content/Skins/Cast/"+e.file;if(!File.Exists(path))path="Content/Skins/"+e.file;
                var pack=SkinCatalog.ReadPack(File.ReadAllBytes(path),e);
                var root=new GameObject("Cast skin validation "+e.id);
                try
                {
                    AvatarArt.Make(root.transform,e.hero,Color.cyan);
                    var visual=SkinRigProfile.Visual(root.transform,e.hero);
                    if(visual==null)throw new Exception("Skin hero visual missing "+e.id);
                    int colliderCount=root.GetComponentsInChildren<Collider>(true).Length;
                    int bodyCount=root.GetComponentsInChildren<Rigidbody>(true).Length;
                    var baseline=AvatarArt.PoseBounds(root.transform);
                    foreach(var binding in pack.bindings??new SkinPalettePatch[0])
                    {
                        bool match=false;
                        foreach(var renderer in visual.GetComponentsInChildren<Renderer>(true))
                            foreach(var m in renderer.sharedMaterials)if(m!=null&&SkinRigProfile.MatchesMaterial(e.hero,m.name,binding.material))match=true;
                        if(!match)throw new Exception("Unmatched skin material "+e.id+": "+binding.material);
                    }
                    SkinVisual.Apply(root.transform,pack);
                    if(root.GetComponentsInChildren<Collider>(true).Length!=colliderCount||root.GetComponentsInChildren<Rigidbody>(true).Length!=bodyCount)
                        throw new Exception("Cosmetic changed physics "+e.id);
                    foreach(var part in pack.meshes)
                    {
                        Transform attachment=null;
                        foreach(var node in visual.GetComponentsInChildren<Transform>(true))if(node.name==part.name)attachment=node;
                        if(attachment==null||attachment.parent.name!=(pack.schema==1?"Body":part.anchor))throw new Exception("Unbound skin part "+e.id+" / "+part.name);
                    }
                    var bounds=AvatarArt.PoseBounds(root.transform);
                    if(bounds.size.magnitude>baseline.size.magnitude*1.28f)throw new Exception("Skin silhouette exceeds budget "+e.id);
                    foreach(var renderer in visual.GetComponentsInChildren<Renderer>(true))foreach(var m in renderer.sharedMaterials)
                    {
                        if(m==null||m.shader==null||m.shader.name=="Hidden/InternalErrorShader"||m.color.a<.999f||m.renderQueue>=3000)
                            throw new Exception("Invalid opaque skin material "+e.id);
                    }
                    var animator=visual.GetComponent<Animator>();
                    var clips=animator.runtimeAnimatorController.animationClips;
                    foreach(string action in new[]{"Idle","Run","Jump","Special","Victory"})
                    {
                        AnimationClip clip=null;
                        foreach(var candidate in clips)
                            if(candidate.name==action||candidate.name.EndsWith("|"+action)||(action=="Victory"&&candidate.name==SkinRigProfile.Names[e.hero]+" Stable Victory"))clip=candidate;
                        if(clip==null)throw new Exception("Missing skin animation "+e.id+" / "+action);
                        foreach(float t in new[]{.0f,.37f,.73f})
                        {
                            clip.SampleAnimation(visual.gameObject,clip.length*t);
                            foreach(var part in pack.meshes)
                            {
                                Transform attachment=null;foreach(var node in visual.GetComponentsInChildren<Transform>(true))if(node.name==part.name)attachment=node;
                                if(attachment==null||!SkinRigProfile.AllowsAnchor(e.hero,attachment.parent.name))throw new Exception("Animated skin detached "+e.id);
                                Vector3 p=attachment.position;if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||p.magnitude>8f)throw new Exception("Invalid animated attachment "+e.id);
                            }
                            poses++;
                        }
                    }
                    // Applying an outfit after an animation has already started
                    // must match applying it at rest and then animating it.
                    AnimationClip run=null;foreach(var clip in clips)if(clip.name=="Run"||clip.name.EndsWith("|Run"))run=clip;
                    run.SampleAnimation(visual.gameObject,run.length*.37f);
                    var lateRoot=new GameObject("Late skin comparison");
                    try
                    {
                        AvatarArt.Make(lateRoot.transform,e.hero,Color.cyan);
                        var lateVisual=SkinRigProfile.Visual(lateRoot.transform,e.hero);
                        run.SampleAnimation(lateVisual.gameObject,run.length*.37f);SkinVisual.Apply(lateRoot.transform,pack);
                        foreach(var part in pack.meshes)
                        {
                            MeshFilter early=null,late=null;
                            foreach(var filter in visual.GetComponentsInChildren<MeshFilter>())if(filter.name==part.name)early=filter;
                            foreach(var filter in lateVisual.GetComponentsInChildren<MeshFilter>())if(filter.name==part.name)late=filter;
                            if(early==null||late==null)throw new Exception("Late skin attachment missing "+e.id);
                            var a=early.sharedMesh.vertices;var b=late.sharedMesh.vertices;
                            foreach(int index in new[]{0,a.Length/2,a.Length-1})
                                if(Vector3.Distance(early.transform.TransformPoint(a[index]),late.transform.TransformPoint(b[index]))>.0002f)
                                    throw new Exception("Late skin bind moved "+e.id+" / "+part.name);
                        }
                    }
                    finally {UnityEngine.Object.DestroyImmediate(lateRoot);}
                    skins++;heroCounts[e.hero]++;Debug.Log("CAST_SKIN_HERO_PASS hero="+e.hero+" id="+e.id+" poses=15 attachments="+pack.meshes.Length+" physicsUnchanged=true");
                }
                finally {UnityEngine.Object.DestroyImmediate(root);}
            }
            foreach(int count in heroCounts)if(count!=5)throw new Exception("Incomplete five-per-hero skin cast");
            Debug.Log("CAST_SKIN_SMOKE_PASS heroes=13 skins="+skins+" sampledPoses="+poses+" lateBindCases="+skins);
        }
    }
}
#endif
