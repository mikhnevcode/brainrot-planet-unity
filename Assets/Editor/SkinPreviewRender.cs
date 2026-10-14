#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace BrainrotPlanet.Editor
{
    public static class SkinPreviewRender
    {
        public static void Run()
        {
            SkinCatalogSmoke.Run();
            CastSkinSmoke.Run();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientLight=new Color(.6f,.65f,.75f);
            var light=new GameObject("Key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.transform.rotation=Quaternion.Euler(40,-30,0);
            var camera=new GameObject("Camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.055f,.10f);camera.fieldOfView=30;
            var target=new RenderTexture(1000,1000,24);camera.targetTexture=target;
            camera.orthographic=true;
            var captionObject=new GameObject("Skin preview caption");captionObject.transform.SetParent(camera.transform,false);
            var caption=captionObject.AddComponent<TextMesh>();caption.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            caption.fontSize=36;caption.anchor=TextAnchor.LowerCenter;caption.alignment=TextAlignment.Center;caption.color=new Color(.88f,.93f,1f);
            captionObject.GetComponent<MeshRenderer>().sharedMaterial=caption.font.material;
            var sheets=new Texture2D[SkinRigProfile.HeroCount];var sheetColumns=new int[SkinRigProfile.HeroCount];
            for(int hero=0;hero<sheets.Length;hero++)sheets[hero]=new Texture2D(1500,300,TextureFormat.RGB24,false);
            var tileTarget=new RenderTexture(300,300,24);
            Directory.CreateDirectory("Builds/CastSkinsPreview");
            var manifest=SkinCatalog.ReadManifest(File.ReadAllText("Content/Skins/catalog-cast-v1.json"));
            foreach(var entry in manifest.skins)
            {
                var root=new GameObject(entry.id);AvatarArt.Make(root.transform,entry.hero,new Color(.14f,.55f,.45f));
                root.transform.rotation=AvatarArt.AlignFacing(AvatarArt.LocalFacing(root.transform),Vector3.forward);
                foreach(var renderer in root.GetComponentsInChildren<Renderer>())if(renderer.name=="Paint aura")renderer.enabled=false;
                string file="Content/Skins/Cast/"+entry.file;if(!File.Exists(file))file="Content/Skins/"+entry.file;
                SkinVisual.Apply(root.transform,SkinCatalog.ReadPack(File.ReadAllBytes(file),entry));
                var bounds=AvatarArt.PoseBounds(root.transform);
                for(int view=0;view<3;view++)
                {
                    Vector3 direction=view==0?new Vector3(1.1f,.65f,2.4f):view==1?new Vector3(.4f,2.5f,1.1f):new Vector3(-1.7f,.6f,-2f);
                    camera.transform.position=bounds.center+direction.normalized*5f;
                    camera.orthographicSize=Mathf.Max(bounds.extents.y,Mathf.Max(bounds.extents.x,bounds.extents.z))*(entry.hero==0||entry.hero==1||entry.hero==3?1.20f:1.35f);
                    caption.text=entry.title;caption.characterSize=camera.orthographicSize*.045f;
                    captionObject.transform.localPosition=new Vector3(0,-camera.orthographicSize*.96f,4f);
                    camera.transform.LookAt(bounds.center);camera.Render();
                    float captionWidth=captionObject.GetComponent<MeshRenderer>().bounds.size.magnitude;
                    if(captionWidth>camera.orthographicSize*1.75f)
                    {caption.characterSize*=camera.orthographicSize*1.75f/captionWidth;camera.Render();}
                    RenderTexture.active=target;var image=new Texture2D(1000,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1000,1000),0,0);image.Apply();
                    File.WriteAllBytes("Builds/CastSkinsPreview/"+entry.id+"-"+view+".png",image.EncodeToPNG());Object.DestroyImmediate(image);RenderTexture.active=null;
                    if(view==0)
                    {
                        Graphics.Blit(target,tileTarget);RenderTexture.active=tileTarget;
                        var tile=new Texture2D(300,300,TextureFormat.RGB24,false);tile.ReadPixels(new Rect(0,0,300,300),0,0);tile.Apply();
                        sheets[entry.hero].SetPixels(sheetColumns[entry.hero]*300,0,300,300,tile.GetPixels());sheetColumns[entry.hero]++;
                        Object.DestroyImmediate(tile);RenderTexture.active=null;
                    }
                }
                Object.DestroyImmediate(root);
            }
            for(int hero=0;hero<sheets.Length;hero++)
            {
                sheets[hero].Apply();File.WriteAllBytes("Builds/CastSkinsPreview/collection-"+SkinRigProfile.Names[hero]+".png",sheets[hero].EncodeToPNG());Object.DestroyImmediate(sheets[hero]);
            }
            Object.DestroyImmediate(tileTarget);
            camera.targetTexture=null;Object.DestroyImmediate(target);
            Debug.Log("SKIN_PREVIEW_RENDER_PASS");
        }
    }
}
#endif
