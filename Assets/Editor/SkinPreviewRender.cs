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
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientLight=new Color(.6f,.65f,.75f);
            var light=new GameObject("Key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.transform.rotation=Quaternion.Euler(40,-30,0);
            var camera=new GameObject("Camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.055f,.10f);camera.fieldOfView=30;
            var target=new RenderTexture(1000,800,24);camera.targetTexture=target;
            Directory.CreateDirectory("Builds/CosmeticsPreview");
            var manifest=SkinCatalog.ReadManifest(File.ReadAllText("Content/Skins/catalog-v2.json"));
            foreach(var entry in manifest.skins)
            {
                var root=new GameObject(entry.id);AvatarArt.Make(root.transform,1,Color.magenta);
                foreach(var t in root.GetComponentsInChildren<Transform>())if(t.name=="Body"||t.name=="Jaw"||t.name=="Tail")Debug.Log("SKIN_BONE "+t.name+" world="+t.position.ToString("F3"));
                SkinVisual.Apply(root.transform,SkinCatalog.ReadPack(File.ReadAllBytes("Content/Skins/"+entry.file),entry));
                var bounds=AvatarArt.PoseBounds(root.transform);
                for(int view=0;view<2;view++)
                {
                    camera.transform.position=bounds.center+(view==0?new Vector3(1.3f,1.0f,2.3f):new Vector3(.4f,2.5f,1.1f))*1.1f;
                    camera.transform.LookAt(bounds.center);camera.Render();
                    RenderTexture.active=target;var image=new Texture2D(1000,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1000,800),0,0);image.Apply();
                    File.WriteAllBytes("Builds/CosmeticsPreview/"+entry.id+"-"+view+".png",image.EncodeToPNG());Object.DestroyImmediate(image);RenderTexture.active=null;
                }
                Object.DestroyImmediate(root);
            }
            camera.targetTexture=null;Object.DestroyImmediate(target);
            Debug.Log("SKIN_PREVIEW_RENDER_PASS");
        }
    }
}
#endif
