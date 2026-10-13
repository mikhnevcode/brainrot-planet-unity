using System.Collections.Generic;
using UnityEngine;
namespace BrainrotPlanet
{
    public sealed class SkinVisual : MonoBehaviour
    {
        private readonly List<Object> owned=new List<Object>();
        public static void Apply(Transform avatar,SkinPack pack)
        {
            if(pack==null)return;
            Transform visual=null,body=null;
            foreach(var t in avatar.GetComponentsInChildren<Transform>(true))
            {if(t.name.StartsWith("Bombardiro crocodile airplane"))visual=t;}
            if(visual==null)return;
            var resources=visual.gameObject.AddComponent<SkinVisual>();
            foreach(var t in visual.GetComponentsInChildren<Transform>(true))if(t.name=="Body")body=t;
            foreach(var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var original=materials[i];if(original==null)continue;
                    string n=original.name.ToLowerInvariant(),color=null;
                    if(n.Contains("brushed silver"))color=pack.hull;
                    else if(n.Contains("metal dark"))color=pack.trim;
                    else if(n.Contains("silver edge")||n.Contains("propeller yellow")||n.Contains("paint tank"))color=pack.accent;
                    else if(n.Contains("cockpit"))color=pack.accent;
                    else if(n.Contains("sculpted textured skin"))color=pack.skin;
                    if(color==null)continue;
                    var material=new Material(original);material.color=SkinCatalog.ParseColor(color);
                    material.name="Cosmetic "+pack.id+" / "+original.name;
                    resources.owned.Add(material);materials[i]=material;
                }
                renderer.sharedMaterials=materials;
            }
            foreach(var part in pack.meshes)
            {
                var mesh=new Mesh{name=pack.id+" / "+part.name};
                var vertices=new Vector3[part.vertices.Length/3];
                for(int i=0;i<vertices.Length;i++)vertices[i]=new Vector3(part.vertices[i*3],part.vertices[i*3+1],part.vertices[i*3+2]);
                mesh.vertices=vertices;mesh.triangles=part.triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
                var go=new GameObject(part.name);go.transform.SetParent(visual,false);
                go.layer=visual.gameObject.layer;
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var material=Colors.New(SkinCatalog.ParseColor(part.color),part.glow);
                material.SetFloat("_Glossiness",.25f);
                go.AddComponent<MeshRenderer>().sharedMaterial=material;
                // Authoring coordinates are model-local. Bind to the existing animated
                // body while retaining the rest-pose placement. No new physics objects.
                if(body!=null)go.transform.SetParent(body,true);
                resources.owned.Add(mesh);resources.owned.Add(material);
            }
            Debug.Log("SKIN_APPLIED id="+pack.id+" parts="+pack.meshes.Length+" physicsUnchanged=true");
        }
        private void OnDestroy(){foreach(var item in owned)if(item!=null){if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}}
    }
}
