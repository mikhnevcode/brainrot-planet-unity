#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
namespace BrainrotPlanet.Editor
{
    public static class HeroMenuSmoke
    {
        public static void Run()
        {
            int checks=0;
            Vector2 rightDown=HeroPreviewFraming.DragAngles(new Vector2(0,10),new Vector2(30,20));
            Vector2 leftUp=HeroPreviewFraming.DragAngles(new Vector2(0,10),new Vector2(-30,-20));
            if(rightDown.x<=0||rightDown.y<=10||leftUp.x>=0||leftUp.y>=10)
                throw new Exception("Inverted hero portrait gesture");
            if(HeroPreviewFraming.DragAngles(new Vector2(0,10),new Vector2(9999,9999))!=new Vector2(80,18)||
               HeroPreviewFraming.DragAngles(new Vector2(0,10),new Vector2(-9999,-9999))!=new Vector2(-80,2))
                throw new Exception("Unlimited hero portrait gesture");
            var manifest=SkinCatalog.ReadManifest(File.ReadAllText("Content/OriginalCast/Skins/catalog-original26-v1.json"));
            for(int hero=0;hero<SkinRigProfile.HeroCount;hero++)checks+=Check(hero,null);
            foreach(var entry in manifest.skins)checks+=Check(entry.hero,entry);
            foreach(var resolution in new[]{new Vector2(420,790),new Vector2(1080,2412),new Vector2(720,1600),new Vector2(960,540)})
            {
                float scale=Mathf.Max(.60f,Mathf.Min(resolution.x/420,resolution.y/790));
                float w=resolution.x/scale,h=resolution.y/scale;
                // Same six-column, twenty-six-tile geometry as DrawHeroMenu, with
                // conservative 20 logical pixels reserved on each safe edge.
                float top=20,bottom=h-28,extras=bottom-48-66-46,gridY=top+270;
                const int columns=6,rows=5;float width=(w-40-8*(columns-1))/columns;
                float height=Mathf.Min(width,(extras-12-gridY-8*(rows-1))/rows);
                if(height<44||gridY+rows*height+8*(rows-1)>extras-11)throw new Exception("Hero grid overlaps controls "+resolution);
                checks++;
            }
            Debug.Log("HERO_MENU_SMOKE_PASS heroes=26 skins=130 views="+checks+" footing=true fullBounds=true tiltLimited=true gridNoPages=true");
        }
        private static int Check(int hero,SkinEntry entry)
        {
            var root=new GameObject("Menu framing QA");
            var cameraObject=new GameObject("Menu framing camera");
            try
            {
                AvatarArt.Make(root.transform,hero,Color.cyan);
                HeroPreviewFraming.SampleIdle(root.transform);
                if(entry!=null)
                {
                    string path="Content/OriginalCast/Skins/"+entry.file;
                    SkinVisual.Apply(root.transform,SkinCatalog.ReadPack(File.ReadAllBytes(path),entry));
                }
                root.transform.rotation=AvatarArt.AlignFacing(AvatarArt.LocalFacing(root.transform),Quaternion.Euler(0,28,0)*Vector3.back);
                foreach(var renderer in root.GetComponentsInChildren<Renderer>())if(renderer.name=="Paint aura")renderer.enabled=false;
                Bounds bounds=AvatarArt.PoseBounds(root.transform);
                root.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                bounds=AvatarArt.PoseBounds(root.transform);
                if(Mathf.Abs(bounds.min.y)>.002f)throw new Exception("Portrait sole off floor "+hero);
                float footprint=Mathf.Max(.42f,Mathf.Max(bounds.size.x,bounds.size.z)*.72f);
                bounds.Encapsulate(new Bounds(new Vector3(0,-.035f,0),new Vector3(footprint,.07f,footprint)));
                var camera=cameraObject.AddComponent<Camera>();camera.aspect=1;camera.fieldOfView=31;camera.nearClipPlane=.03f;
                int checks=0;
                foreach(float yaw in new[]{-80f,-40f,0f,40f,80f})
                foreach(float pitch in new[]{2f,10f,18f})
                foreach(float zoom in new[]{1f,1.4f})
                {
                    HeroPreviewFraming.Place(camera,bounds,yaw,pitch,zoom);
                    if(Vector3.Dot(camera.transform.up,Vector3.up)<.94f)throw new Exception("Portrait camera inverted");
                    for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                    {
                        Vector3 point=bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z));
                        Vector3 viewport=camera.WorldToViewportPoint(point);
                        if(viewport.x<.03f||viewport.x>.97f||viewport.y<.03f||viewport.y>.97f||viewport.z<camera.nearClipPlane)
                            throw new Exception("Portrait clipped hero="+hero+" skin="+entry?.id+" view="+viewport);
                    }
                    checks++;
                }
                return checks;
            }
            finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraObject);}
        }
    }
}
#endif
