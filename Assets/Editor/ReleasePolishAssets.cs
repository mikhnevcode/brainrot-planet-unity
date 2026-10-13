#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BrainrotPlanet.Editor
{
    public static class ReleasePolishAssets
    {
        private static readonly string[] Names={"Tralalero","Bombardiro","Frigo","Bombombini","Ballerina","Assassino","Patapim","TungSahur","Lirili","Frulli","Boneca","Trippi","Troppa"};
        private static string Model(int i)=>"Assets/Resources/Characters/"+Names[i]+"_"+(i==0?"v05":i==1?"v06":i==12?"v01":"v04")+".fbx";
        public static void Prepare()
        {
            const string dir="Assets/Resources/Characters/Victory064";
            Directory.CreateDirectory(dir);
            foreach(string name in Names)
                File.Copy("Characters/Victory064/"+name+"_stage064.fbx",dir+"/"+name+"_stage064.fbx",true);
            AssetDatabase.Refresh();
            for(int i=0;i<Names.Length;i++)
            {
                string path=dir+"/"+Names[i]+"_stage064.fbx";
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                importer.animationType=ModelImporterAnimationType.Generic;
                importer.importAnimation=true;
                importer.animationCompression=ModelImporterAnimationCompression.Off;
                importer.SaveAndReimport();
                AnimationClip source=null;
                foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    if(asset is AnimationClip clip&&!clip.name.StartsWith("__preview")){source=clip;break;}
                if(source==null)throw new Exception("No stage clip "+path);
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(Model(i));
                var result=new AnimationClip{name=Names[i]+" Stable Victory",frameRate=30};
                foreach(var binding in AnimationUtility.GetCurveBindings(source))
                {
                    var mapped=binding;
                    // Armature-only FBX may have a different root wrapper. Bind by
                    // unique bone name against the actual renderable hero prefab.
                    string leaf=binding.path.Substring(binding.path.LastIndexOf('/')+1);
                    Transform node=null;
                    foreach(var candidate in model.GetComponentsInChildren<Transform>(true))
                        if(candidate.name==leaf){if(node!=null)throw new Exception("Ambiguous bone "+leaf);node=candidate;}
                    if(node==null)throw new Exception("Unbound stage bone "+Names[i]+" "+binding.path);
                    mapped.path=AnimationUtility.CalculateTransformPath(node,model.transform);
                    AnimationUtility.SetEditorCurve(result,mapped,AnimationUtility.GetEditorCurve(source,binding));
                }
                var settings=AnimationUtility.GetAnimationClipSettings(result);settings.loopTime=true;
                AnimationUtility.SetAnimationClipSettings(result,settings);
                string output=dir+"/"+Names[i]+".anim";
                var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(output);
                if(existing!=null){EditorUtility.CopySerialized(result,existing);UnityEngine.Object.DestroyImmediate(result);result=existing;}
                else AssetDatabase.CreateAsset(result,output);
                var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Resources/Characters/"+Names[i]+".controller");
                bool attached=false;
                foreach(var state in controller.layers[0].stateMachine.states)
                    if(state.state.name=="Victory"){state.state.motion=result;attached=true;}
                if(!attached)throw new Exception("Missing Victory state "+Names[i]);
                EditorUtility.SetDirty(controller);
                Debug.Log("STAGE_CLIP_BOUND hero="+Names[i]+" curves="+AnimationUtility.GetCurveBindings(result).Length);
            }
            AssetDatabase.SaveAssets();
            Verify();
            VerifyFraming();
            Grain();
            Portraits();
        }
        public static void Verify()
        {
            var territory=new TerritorySystem(null);
            Vector3 point=PlanetMath.FromLonLat(.3f,.5f);
            territory.TraceStep(1,point);
            int before=territory.Trails[1].Count,events=0;
            territory.OnTrailDestroyed+=(team,at)=>{if(team==1)events++;};
            territory.IsTrailProtected=team=>team==1;
            territory.TraceStep(2,point);
            if(before==0||territory.Trails[1].Count!=before||events!=0)throw new Exception("Protected trail was mutated");
            territory.IsTrailProtected=team=>false;
            territory.TraceStep(2,point);
            if(territory.Trails[1].Count!=0||events!=1)throw new Exception("Unprotected trail not cut exactly once");
            Debug.Log("TRAIL_PROTECTION_SMOKE_PASS protected=intact unprotected=cut events=1");
            for(int i=0;i<Names.Length;i++)
            {
                var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Model(i)));
                try
                {
                    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Resources/Characters/Victory064/"+Names[i]+".anim");
                    Transform body=null;
                    foreach(var node in model.GetComponentsInChildren<Transform>())if(node.name=="Body")body=node;
                    if(body==null)throw new Exception("Missing Body "+Names[i]);
                    clip.SampleAnimation(model,0);Quaternion rest=body.localRotation;
                    float worst=0;
                    for(int f=0;f<=72;f++){clip.SampleAnimation(model,clip.length*f/72f);worst=Mathf.Max(worst,Quaternion.Angle(rest,body.localRotation));}
                    if(worst>20)throw new Exception("Victory body rolls "+Names[i]+" "+worst);
                    Debug.Log("STAGE_POSE_SMOKE_PASS hero="+Names[i]+" maxBodyDegrees="+worst.ToString("F2"));
                }
                finally{UnityEngine.Object.DestroyImmediate(model);}
            }
        }
        private static void Layer(Transform node){node.gameObject.layer=30;foreach(Transform child in node)Layer(child);}
        private static void VerifyFraming()
        {
            for(int i=0;i<Names.Length;i++)
            {
                var root=new GameObject("Framing regression "+Names[i]);
                try
                {
                    AvatarArt.Make(root.transform,i,Color.magenta);
                    root.transform.rotation=AvatarArt.AlignFacing(AvatarArt.LocalFacing(root.transform),Vector3.back);
                    Bounds origin=AvatarArt.PoseBounds(root.transform);
                    float size=Mathf.Max(origin.size.x,Mathf.Max(origin.size.y,origin.size.z));
                    if(size<.2f||size>4f)throw new Exception("Incorrect skinned scale "+Names[i]+" "+origin);
                    Vector3 offset=new Vector3(0,38,-55);root.transform.position=offset;
                    Bounds moved=AvatarArt.PoseBounds(root.transform);
                    if(Vector3.Distance(origin.size,moved.size)>.005f||Vector3.Distance(origin.center+offset,moved.center)>.005f)
                        throw new Exception("World translation changes pose bounds "+Names[i]);
                    var animator=root.GetComponentInChildren<Animator>();
                    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Resources/Characters/Victory064/"+Names[i]+".anim");
                    clip.SampleAnimation(animator.gameObject,0);
                    Bounds first=AvatarArt.PoseBounds(root.transform);
                    if(Vector3.Distance(first.center,moved.center)>.08f||Vector3.Distance(first.size,moved.size)>.10f)
                        throw new Exception("Victory FBX units do not match render rig "+Names[i]+" rest="+moved+" first="+first);
                    float lowest=first.min.y,highest=first.max.y,minFace=1;
                    for(int frame=0;frame<=72;frame+=6)
                    {
                        clip.SampleAnimation(animator.gameObject,clip.length*frame/72f);
                        Bounds pose=AvatarArt.PoseBounds(root.transform);
                        lowest=Mathf.Min(lowest,pose.min.y);highest=Mathf.Max(highest,pose.max.y);
                        minFace=Mathf.Min(minFace,Vector3.Dot(root.transform.TransformDirection(AvatarArt.LocalFacing(root.transform)),Vector3.back));
                    }
                    if(lowest<moved.min.y-.15f||highest>moved.max.y+.40f||minFace<.85f)
                        throw new Exception("Victory dance leaves stable stage "+Names[i]+" floorDip="+(moved.min.y-lowest)+" minFace="+minFace);
                    Debug.Log("STAGE_ANIMATED_FRAMING_PASS hero="+Names[i]+" floorDip="+(moved.min.y-lowest).ToString("F3")+" minFace="+minFace.ToString("F3"));
                    Debug.Log("STAGE_FRAMING_SMOKE_PASS hero="+Names[i]+" worldSize="+origin.size);
                }
                finally{UnityEngine.Object.DestroyImmediate(root);}
            }
        }
        private static void Grain()
        {
            const int size=128;
            var texture=new Texture2D(size,size,TextureFormat.RGB24,false);
            var pixels=new Color[size*size];
            var random=new System.Random(64013);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=x/(float)size,v=y/(float)size;
                float a=Mathf.Lerp(Mathf.PerlinNoise(12+u*6,14+v*6),Mathf.PerlinNoise(6+u*6,14+v*6),u);
                float b=Mathf.Lerp(Mathf.PerlinNoise(12+u*6,8+v*6),Mathf.PerlinNoise(6+u*6,8+v*6),u);
                float value=.30f+Mathf.Lerp(a,b,v)*.25f+(float)random.NextDouble()*.35f;
                pixels[y*size+x]=new Color(value,value,value);
            }
            texture.SetPixels(pixels);texture.Apply();
            const string path="Assets/Resources/PlanetGrain064.png";
            File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=false;
            importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        }
        private static void Portraits()
        {
            const int tile=192;
            var atlas=new Texture2D(tile*4,tile*4,TextureFormat.RGBA32,false);
            atlas.SetPixels(new Color[tile*tile*16]);
            var rig=new GameObject("Atlas render rig");
            var camera=rig.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<30;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
            camera.fieldOfView=30;camera.nearClipPlane=.02f;camera.farClipPlane=2000;
            var rt=new RenderTexture(tile,tile,24);rt.antiAliasing=4;rt.Create();camera.targetTexture=rt;
            var light=rig.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.35f;light.cullingMask=1<<30;
            var previous=RenderTexture.active;
            try
            {
                for(int i=0;i<Names.Length;i++)
                {
                    var actor=new GameObject("Portrait "+Names[i]);
                    try
                    {
                        actor.transform.position=new Vector3(800,0,0);AvatarArt.Make(actor.transform,i,Colors.Team(1));
                        actor.transform.rotation=Quaternion.AngleAxis(18,Vector3.up)*AvatarArt.AlignFacing(AvatarArt.LocalFacing(actor.transform),Vector3.back);
                        Layer(actor.transform);bool first=true;Bounds bounds=new Bounds();
                        foreach(var renderer in actor.GetComponentsInChildren<Renderer>())
                        {
                            if(renderer.name=="Paint aura"){renderer.enabled=false;continue;}
                            if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);
                        }
                        float size=Mathf.Max(bounds.size.y,Mathf.Max(bounds.size.x,bounds.size.z));
                        camera.transform.position=bounds.center+new Vector3(0,size*.10f,-size*2.15f);
                        camera.transform.LookAt(bounds.center);camera.Render();
                        RenderTexture.active=rt;
                        atlas.ReadPixels(new Rect(0,0,tile,tile),(i%4)*tile,(3-i/4)*tile,false);
                    }
                    finally{UnityEngine.Object.DestroyImmediate(actor);}
                }
                atlas.Apply();File.WriteAllBytes("Assets/Resources/HeroPortraits064.png",atlas.EncodeToPNG());
                AssetDatabase.ImportAsset("Assets/Resources/HeroPortraits064.png");
                var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/HeroPortraits064.png");
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=1024;importer.SaveAndReimport();
                Debug.Log("PORTRAIT_ATLAS_READY heroes=13 size=768");
            }
            finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(rig);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(atlas);}
        }
    }
}
#endif
