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
            Transform visual=SkinRigProfile.Visual(avatar,pack.hero),body=null;
            if(visual==null)return;
            if(visual.GetComponent<SkinVisual>()!=null)return;
            var resources=visual.gameObject.AddComponent<SkinVisual>();
            var rest=visual.GetComponent<SkinRestBindings>();
            var anchors=new Dictionary<string,Transform>();
            foreach(var t in visual.GetComponentsInChildren<Transform>(true))if(SkinRigProfile.AllowsAnchor(pack.hero,t.name))anchors[t.name]=t;
            anchors.TryGetValue("Body",out body);
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
                    SkinPalettePatch patch=null;
                    if(pack.schema==2)
                    {
                        color=null;
                        foreach(var binding in pack.bindings)if(SkinRigProfile.MatchesMaterial(pack.hero,original.name,binding.material)){patch=binding;color=binding.color;break;}
                    }
                    if(color==null)continue;
                    var material=new Material(original);material.color=SkinCatalog.ParseColor(color);
                    if(patch!=null)
                    {
                        if(material.HasProperty("_Metallic"))material.SetFloat("_Metallic",patch.metallic);
                        if(material.HasProperty("_Glossiness"))material.SetFloat("_Glossiness",patch.smoothness);
                        if(patch.glow){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",material.color*.5f);}
                    }
                    material.name="Cosmetic "+pack.id+" / "+original.name;
                    resources.owned.Add(material);materials[i]=material;
                }
                renderer.sharedMaterials=materials;
            }
            foreach(var part in pack.meshes)
            {
                string anchorName=pack.schema==1?"Body":part.anchor;
                if(rest==null||!rest.TryGet(anchorName,out var bind)||!anchors.TryGetValue(anchorName,out var anchor))
                {Debug.LogError("SKIN_BIND_MISSING id="+pack.id+" anchor="+anchorName);continue;}
                var mesh=new Mesh{name=pack.id+" / "+part.name};
                var vertices=new Vector3[part.vertices.Length/3];
                for(int i=0;i<vertices.Length;i++)vertices[i]=bind.MultiplyPoint3x4(new Vector3(part.vertices[i*3],part.vertices[i*3+1],part.vertices[i*3+2]));
                mesh.vertices=vertices;mesh.triangles=part.triangles;
                if(part.normals!=null&&part.normals.Length==part.vertices.Length)
                {
                    var normals=new Vector3[vertices.Length];
                    var normalMatrix=bind.inverse.transpose;
                    for(int i=0;i<normals.Length;i++)normals[i]=normalMatrix.MultiplyVector(new Vector3(part.normals[i*3],part.normals[i*3+1],part.normals[i*3+2])).normalized;
                    mesh.normals=normals;
                }
                else mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                var go=new GameObject(part.name);go.transform.SetParent(anchor,false);
                go.layer=visual.gameObject.layer;
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var material=Colors.New(SkinCatalog.ParseColor(part.color),part.glow);
                material.SetFloat("_Glossiness",.25f);
                go.AddComponent<MeshRenderer>().sharedMaterial=material;
                // Vertices are already in the captured bone rest space. This also
                // places a skin correctly when downloaded during an animated pose.
                resources.owned.Add(mesh);resources.owned.Add(material);
            }
            Debug.Log("SKIN_APPLIED id="+pack.id+" parts="+pack.meshes.Length+" physicsUnchanged=true");
        }
        private void OnDestroy(){foreach(var item in owned)if(item!=null){if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}}
    }
}
