using UnityEngine;
namespace BrainrotPlanet
{
    public sealed class PlanetGeneratedArt : MonoBehaviour
    {
        private Mesh mesh;private Material material;
        public void Own(Mesh generatedMesh,Material generatedMaterial){mesh=generatedMesh;material=generatedMaterial;}
        private void OnDestroy()
        {
            if(mesh!=null){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}
            if(material!=null){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}
        }
    }
    public sealed partial class PlanetGame
    {
        private void CreatePlanetCompanion()
        {
            var companion=new GameObject("Distant "+Arena.Kind+" companion planet");
            companion.transform.SetParent(worldRoot,false);
            Vector3 opening=new Vector3(.91f,.20f,.33f).normalized;
            PlanetBiomeRules.Basis(opening,out Vector3 east,out Vector3 north);
            companion.transform.position=-opening*120+east*57+north*52;
            const int lon=40,lat=24;
            var vertices=new Vector3[(lon+1)*(lat+1)];var colors=new Color[vertices.Length];var normals=new Vector3[vertices.Length];
            var triangles=new int[lon*lat*6];
            Color baseColor=Arena.Kind==BattlePlanet.Tropical?new Color(.48f,.37f,.72f):Arena.Kind==BattlePlanet.Desert?new Color(.79f,.34f,.21f):new Color(.17f,.45f,.87f);
            for(int y=0;y<=lat;y++)for(int x=0;x<=lon;x++)
            {
                int k=y*(lon+1)+x;Vector3 n=PlanetMath.FromLonLat(x/(float)lon,y/(float)lat);
                vertices[k]=n*17;normals[k]=n;
                float band=.5f+.5f*Mathf.Sin(n.y*22+n.x*2);
                colors[k]=Color.Lerp(baseColor,Color.Lerp(baseColor,Color.white,.65f),band*.55f);
                if(Arena.Kind==BattlePlanet.Moon)
                {
                    float land=Mathf.PerlinNoise(n.x*4+3,n.z*4+n.y*2+8);
                    if(land>.55f)colors[k]=new Color(.3f,.65f,.47f);
                    if(band>.93f)colors[k]=Color.Lerp(colors[k],Color.white,.65f);
                }
            }
            int index=0;for(int y=0;y<lat;y++)for(int x=0;x<lon;x++)
            {
                int a=y*(lon+1)+x,b=a+1,c=a+lon+1,d=c+1;
                triangles[index++]=a;triangles[index++]=c;triangles[index++]=b;triangles[index++]=b;triangles[index++]=c;triangles[index++]=d;
            }
            var mesh=new Mesh{name="Distant companion sphere"};mesh.vertices=vertices;mesh.colors=colors;mesh.triangles=triangles;mesh.normals=normals;mesh.RecalculateBounds();
            var material=new Material(Resources.Load<Shader>("PlanetBiomeDecor"));
            companion.AddComponent<MeshFilter>().sharedMesh=mesh;
            companion.AddComponent<MeshRenderer>().sharedMaterial=material;
            companion.AddComponent<PlanetGeneratedArt>().Own(mesh,material);
        }
    }
}
