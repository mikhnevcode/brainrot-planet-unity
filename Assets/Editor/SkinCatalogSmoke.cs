using System;
using System.IO;
using System.Text;
using UnityEngine;
namespace BrainrotPlanet.Editor
{
    public static class SkinCatalogSmoke
    {
        private static int cases;
        private static void Check(bool value){if(!value)throw new Exception("Cosmetics check failed");cases++;}
        private static void Reject(Action action){bool rejected=false;try{action();}catch(InvalidDataException){rejected=true;}Check(rejected);}
        public static void Run()
        {
            cases=0;
            foreach(string value in new[]{"../escape","https://other-host","a/b","","spaces not valid"})Check(!SkinCatalog.ValidId(value));
            Check(SkinCatalog.ValidReference(""));Check(!SkinCatalog.ValidReference("bombardiro_orbital@bad"));
            var manifest=SkinCatalog.ReadManifest(File.ReadAllText("Content/Skins/catalog-v2.json"));Check(manifest.skins.Length==2);
            foreach(var entry in manifest.skins)
            {
                var data=File.ReadAllBytes("Content/Skins/"+entry.file);var pack=SkinCatalog.ReadPack(data,entry);
                Check(pack.meshes.Length<=4);Check(pack.hero==1);Check(SkinCatalog.ValidReference(entry.id+"@"+entry.sha256));
                var damaged=(byte[])data.Clone();damaged[10]^=1;Reject(()=>SkinCatalog.ReadPack(damaged,entry));
                pack.meshes[0].triangles[0]=int.MaxValue;var invalid=Encoding.UTF8.GetBytes(JsonUtility.ToJson(pack));
                var test=new SkinEntry{id=entry.id,hero=1,bytes=invalid.Length,sha256=SkinCatalog.Hash(invalid)};
                Reject(()=>SkinCatalog.ReadPack(invalid,test));
            }
            Reject(()=>SkinCatalog.ReadManifest("{\"schema\":999,\"revision\":1,\"skins\":[]}"));
            Reject(()=>SkinCatalog.ParseColor("#ffffff00"));
            var wrong=JsonUtility.FromJson<SkinManifest>(JsonUtility.ToJson(manifest));wrong.skins[0].file="../outside.json";
            Reject(()=>SkinCatalog.ReadManifest(JsonUtility.ToJson(wrong)));
            Debug.Log("SKIN_CATALOG_SMOKE_PASS cases="+cases);
        }
    }
}
