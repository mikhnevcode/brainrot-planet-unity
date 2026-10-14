#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
namespace BrainrotPlanet.Editor
{
    public static class StoreSmoke
    {
        public static void Run()
        {
            int cases=0;
            var catalog=JsonUtility.FromJson<StoreCatalog>(File.ReadAllText("Content/Store/catalog-test-v1.json"));
            var snapshot=new StoreSnapshot{schema=1,mode="test",ok=true,catalog=catalog,owned=catalog.free,transactions=Array.Empty<StoreTransaction>()};
            TestStoreClient.Validate(snapshot);cases++;
            if(catalog.products.Length!=99||catalog.free.Length!=13)throw new Exception("Store collection count");cases++;
            foreach(var p in catalog.products){snapshot.owned=p.grants;TestStoreClient.Validate(snapshot);cases++;}
            snapshot.owned=new[]{"damage:boost"};Invalid(snapshot);cases++;
            snapshot.owned=Array.Empty<string>();snapshot.mode="production";Invalid(snapshot);cases++;snapshot.mode="test";
            snapshot.transactions=new[]{new StoreTransaction{tx=new string('a',32),product="trail.stars",status="pending"}};TestStoreClient.Validate(snapshot);cases++;
            snapshot.transactions[0].status="paid";Invalid(snapshot);cases++;
            snapshot.transactions[0].status="success";snapshot.transactions[0].tx="not-a-transaction";Invalid(snapshot);cases++;
            if(ShopCosmetics.ValidGrant("skin:../../secret")||ShopCosmetics.ValidGrant("trail:unknown")||TestStoreClient.ValidToken(new string('a',32)+"\n"))throw new Exception("Store identifiers");cases++;
            var root=new GameObject("Store geometry smoke");
            try
            {
                foreach(var style in new[]{"coral","garden","orbital"})
                {
                    var art=new GameObject(style);art.transform.SetParent(root.transform);art.AddComponent<CosmeticStageArt>().Configure(Vector3.zero,style);
                    if(art.GetComponentsInChildren<Collider>().Length!=0)throw new Exception("Cosmetic stage physics");cases++;
                    foreach(var r in art.GetComponentsInChildren<Renderer>())if(r.sharedMaterial==null)throw new Exception("Cosmetic material");
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
            Debug.Log("STORE_SMOKE_PASS cases="+cases+" products=99 cosmeticsOnly=true");
        }
        private static void Invalid(StoreSnapshot s){try{TestStoreClient.Validate(s);}catch(InvalidDataException){return;}throw new Exception("Invalid store snapshot accepted");}
    }
}
#endif
