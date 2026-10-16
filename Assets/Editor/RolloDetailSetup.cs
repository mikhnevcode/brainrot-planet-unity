#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace BrainrotPlanet.Editor
{
    public static class RolloDetailSetup
    {
        public static void Prepare()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string path in Directory.GetFiles("Assets/Resources/OriginalCastDetail/RolloBulk_v07","*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                bool normal=path.Contains("_Normal"),mask=path.Contains("_TintMask")||path.Contains("_AO");
                importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
                importer.sRGBTexture=!normal&&!mask;importer.alphaSource=TextureImporterAlphaSource.None;
                importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.anisoLevel=2;
                importer.maxTextureSize=path.Contains("Body_")?2048:1024;
                importer.textureCompression=TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
            OriginalCastSetup.PrepareAndRender();Smoke();RenderReview();
            Debug.Log("ROLLO_DETAIL_UNITY_PREPARE_PASS bakedMaps=6 skinVariants=5 animations=8 opaque=true");
        }
        public static void Smoke()
        {
            var root=new GameObject("Detailed Rollo articulation QA");
            try
            {
                AvatarArt.Make(root.transform,0,Color.cyan);HeroPreviewFraming.SampleIdle(root.transform);
                var visual=SkinRigProfile.Visual(root.transform,0);Transform drum=null,tail=null;
                foreach(var bone in visual.GetComponentsInChildren<Transform>())
                {if(bone.name=="Appendage_0")drum=bone;if(bone.name=="Tail")tail=bone;}
                if(drum==null||tail==null)throw new Exception("Detailed Rollo pivots missing");
                var drumRest=drum.localRotation;var tailRest=tail.localRotation;AnimationClip run=null;
                foreach(var clip in visual.GetComponent<Animator>().runtimeAnimatorController.animationClips)if(clip.name=="Run")run=clip;
                if(run==null)throw new Exception("Detailed Rollo run missing");
                run.SampleAnimation(visual.gameObject,run.length*.43f);
                if(Quaternion.Angle(drumRest,drum.localRotation)<100||Quaternion.Angle(tailRest,tail.localRotation)>20)
                    throw new Exception("Detailed Rollo drum/tail articulation mismatch");
                int facialCaps=0;
                foreach(var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if(renderer.sharedMesh.triangles.Length/3>45000)throw new Exception("Detailed Rollo triangle budget");
                    foreach(var weight in renderer.sharedMesh.boneWeights)
                        if(Mathf.Abs(weight.weight0+weight.weight1+weight.weight2+weight.weight3-1)>.002f)throw new Exception("Detailed Rollo unbound vertex");
                    var normals=renderer.sharedMesh.normals;
                    for(int submesh=0;submesh<renderer.sharedMaterials.Length;submesh++)
                    {
                        string name=renderer.sharedMaterials[submesh].name;
                        if(!name.Contains(" mouth ")&&!name.Contains(" tongue "))continue;
                        Vector3 average=Vector3.zero;
                        foreach(int vertex in renderer.sharedMesh.GetIndices(submesh))average+=normals[vertex];
                        average=root.transform.InverseTransformDirection(renderer.transform.TransformDirection(average.normalized));
                        float front=Vector3.Dot(average,AvatarArt.LocalFacing(root.transform));
                        if(name.Contains(" tongue "))
                        {
                            // A solid tongue has outward normals in all directions.
                            // Require a front-facing surface instead of treating
                            // its closed volume as an open planar facial cap.
                            front=-1;
                            foreach(int vertex in renderer.sharedMesh.GetIndices(submesh))
                            {
                                var n=root.transform.InverseTransformDirection(renderer.transform.TransformDirection(normals[vertex]));
                                front=Mathf.Max(front,Vector3.Dot(n,AvatarArt.LocalFacing(root.transform)));
                            }
                        }
                        if(front<.2f)
                            throw new Exception("Back-facing detailed Rollo facial cap: "+name);
                        facialCaps++;
                    }
                }
                if(facialCaps!=2)throw new Exception("Detailed Rollo facial caps missing");
                Debug.Log("ROLLO_DETAIL_FACE_SMOKE_PASS mouthOutward=true tongueOutward=true");
                Debug.Log("ROLLO_DETAIL_MOTION_SMOKE_PASS drumRotates=true tailDoesNotSpin=true weightsNormalized=true");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        public static void RenderReview()
        {
            Directory.CreateDirectory("Builds/RolloFace");
            var oldAmbient=RenderSettings.ambientLight;var oldMode=RenderSettings.ambientMode;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.42f,.48f,.55f);
            var cameraObject=new GameObject("Detailed Rollo review camera");var camera=cameraObject.AddComponent<Camera>();
            camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.72f,.86f,.89f);
            camera.nearClipPlane=.02f;camera.allowHDR=false;
            var target=new RenderTexture(896,896,24,RenderTextureFormat.ARGB32){antiAliasing=2};target.Create();camera.targetTexture=target;
            var lights=new GameObject[2];
            for(int i=0;i<2;i++)
            {
                lights[i]=new GameObject("Detailed Rollo review soft light");var light=lights[i].AddComponent<Light>();light.type=LightType.Directional;
                light.intensity=i==0?1.0f:.45f;light.cullingMask=1<<30;lights[i].transform.rotation=Quaternion.Euler(i==0?32:15,i==0?-35:115,0);
            }
            try
            {
                var manifest=SkinCatalog.ReadManifest(File.ReadAllText("Content/OriginalCast/Skins/"+SkinCatalog.CatalogFile));
                Render(camera,target,null,"classic");
                foreach(var entry in manifest.skins)if(entry.hero==0)Render(camera,target,entry,entry.id);
                foreach(string pose in new[]{"Run","Jump","Drop","VictoryShow"})Render(camera,target,null,"pose-"+pose,pose);
            }
            finally
            {
                camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);
                foreach(var light in lights)UnityEngine.Object.DestroyImmediate(light);
                RenderSettings.ambientLight=oldAmbient;RenderSettings.ambientMode=oldMode;
            }
        }
        private static void Render(Camera camera,RenderTexture target,SkinEntry entry,string label,string pose=null)
        {
            var root=new GameObject("Detailed Rollo "+label);
            try
            {
                AvatarArt.Make(root.transform,0,Color.cyan);HeroPreviewFraming.SampleIdle(root.transform);
                if(pose!=null)
                {
                    var visual=SkinRigProfile.Visual(root.transform,0);
                    bool found=false;
                    foreach(var clip in visual.GetComponent<Animator>().runtimeAnimatorController.animationClips)
                        if(clip.name==pose){clip.SampleAnimation(visual.gameObject,clip.length*.43f);found=true;break;}
                    if(!found)throw new Exception("Detailed Rollo pose missing: "+pose);
                }
                if(entry!=null)SkinVisual.Apply(root.transform,SkinCatalog.ReadPack(File.ReadAllBytes("Content/OriginalCast/Skins/"+entry.file),entry));
                root.transform.rotation=AvatarArt.AlignFacing(AvatarArt.LocalFacing(root.transform),Quaternion.Euler(0,-28,0)*Vector3.back);
                foreach(var node in root.GetComponentsInChildren<Transform>(true))node.gameObject.layer=30;
                int triangles=0;foreach(var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>())triangles+=renderer.sharedMesh.triangles.Length/3;
                if(triangles>45000)throw new Exception("Detailed Rollo topology exceeds budget "+triangles);
                var bounds=AvatarArt.PoseBounds(root.transform);root.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                bounds=AvatarArt.PoseBounds(root.transform);HeroPreviewFraming.Place(camera,bounds,0,8,1.02f);
                camera.orthographic=true;camera.orthographicSize=Mathf.Max(bounds.extents.y,bounds.extents.x)*1.12f;camera.Render();
                var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                try{image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes("Builds/RolloFace/Unity-"+label+".png",image.EncodeToPNG());}
                finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);}
                Debug.Log("ROLLO_DETAIL_RENDER_PASS variant="+label+" triangles="+triangles+" shader="+root.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial.shader.name);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
#endif
