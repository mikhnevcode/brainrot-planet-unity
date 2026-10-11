using UnityEngine;

namespace BrainrotPlanet
{
    public sealed class PlanetSurface : MonoBehaviour
    {
        public MeshCollider GroundCollider { get; private set; }
        public Texture2D MapTexture { get; private set; }
        private Color32[] pixels;
        private Color32[] unpainted;
        private readonly Color32[] teamColors = {
            new Color32(0,0,0,0),new Color32(255,78,169,255),
            new Color32(48,174,255,255),new Color32(255,174,57,255),
            new Color32(109,241,130,255)
        };
        private static readonly Vector3[] mountains = {
            new Vector3(-.55f,.2f,.81f).normalized,
            new Vector3(.62f,-.30f,-.72f).normalized,
            new Vector3(-.04f,-.93f,.36f).normalized,
            new Vector3(-.88f,.36f,-.3f).normalized,
            new Vector3(.27f,.82f,-.5f).normalized
        };

        public static float Elevation(Vector3 direction)
        {
            Vector3 n = direction.normalized;
            float a = Mathf.PerlinNoise(n.x * 6.5f + 14.8f, n.z * 6.5f + n.y * 1.8f + 3.1f);
            float b = Mathf.PerlinNoise(n.y * 13f + 9.2f,n.x * 13f + n.z * 4.8f + 7.3f);
            float height = .08f + .62f * a * a + .15f * b;
            foreach (Vector3 mountain in mountains)
            {
                float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(n, mountain),-1f,1f));
                float q = angle / .115f;
                height += 4.6f * Mathf.Exp(-q*q*1.3f);
            }
            return height;
        }
        public static Vector3 Point(Vector3 direction, float extra = 0)
        {
            Vector3 n = direction.normalized;
            return n * (PlanetMath.Radius + Elevation(n) + extra);
        }
        public void Build()
        {
            gameObject.layer = 8;
            const int lon = 192, lat = 96;
            var v = new Vector3[(lon + 1) * (lat + 1)];
            var uv = new Vector2[v.Length];
            var triangles = new int[lon * lat * 6];
            for(int j=0;j<=lat;j++)for(int i=0;i<=lon;i++)
            {
                float u=(float)i/lon,t=(float)j/lat;
                Vector3 dir=PlanetMath.FromLonLat(u,t);
                int k=j*(lon+1)+i;
                v[k]=Point(dir);
                uv[k]=new Vector2(u,t);
            }
            int z=0;
            for(int j=0;j<lat;j++)for(int i=0;i<lon;i++)
            {
                int a=j*(lon+1)+i,b=a+1,c=a+lon+1,d=c+1;
                triangles[z++]=a;triangles[z++]=c;triangles[z++]=b;
                triangles[z++]=b;triangles[z++]=c;triangles[z++]=d;
            }
            Mesh mesh = new Mesh { name="Relief Planet" };
            mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices=v;mesh.uv=uv;mesh.triangles=triangles;
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            GroundCollider=gameObject.AddComponent<MeshCollider>();GroundCollider.sharedMesh=mesh;
            MapTexture=new Texture2D(PlanetMath.Width,PlanetMath.Height,TextureFormat.RGBA32,false);
            MapTexture.wrapModeU=TextureWrapMode.Repeat;MapTexture.wrapModeV=TextureWrapMode.Clamp;
            MapTexture.filterMode=FilterMode.Bilinear;
            pixels=new Color32[PlanetMath.CellCount];
            unpainted=new Color32[PlanetMath.CellCount];
            PrepareBaseColors();
            var mat = Colors.New(Color.white);mat.name="Planet paint / relief";
            mat.SetFloat("_Glossiness",.18f);mat.SetTexture("_MainTex",MapTexture);
            gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;
            PaintTexture(null,null);
            CreateRockProps();
        }
        private void PrepareBaseColors()
        {
            for(int cell=0;cell<unpainted.Length;cell++)
            {
                Vector3 n=PlanetMath.CellDirection(cell);
                float noise=Mathf.PerlinNoise(n.x*7f+3.4f,n.z*7f+n.y*5f+1.6f);
                float alt=Elevation(n);
                Color baseColor = alt>2.0f ? new Color(.61f,.60f,.69f) :
                    noise>.56f ? new Color(.48f,.79f,.67f) : new Color(.67f,.82f,.72f);
                if(alt>3.7f)baseColor=new Color(.87f,.88f,.95f);
                unpainted[cell]=baseColor;
            }
        }
        public void PaintTexture(byte[] owners, System.Collections.Generic.HashSet<int>[] trails)
        {
            for(int cell=0;cell<pixels.Length;cell++)
            {
                Color baseColor=unpainted[cell];
                byte owner=owners==null ? (byte)0 : owners[cell];
                if(owner>0 && owner<teamColors.Length)
                    baseColor=(Color)teamColors[owner]; // Opaque paint: no underlying terrain patches.
                if(trails!=null)
                {
                    for(int team=1;team<trails.Length;team++)
                        if(trails[team]!=null && trails[team].Contains(cell))
                        {baseColor=Color.Lerp((Color)teamColors[team],Color.white,.30f);break;}
                }
                pixels[cell]=baseColor;
            }
            MapTexture.SetPixels32(pixels);MapTexture.Apply(false);
        }
        private void CreateRockProps()
        {
            Vector3[] spots={
                new Vector3(-.51f,.2f,.86f),new Vector3(-.86f,.36f,-.28f),
                new Vector3(.24f,.84f,-.50f),new Vector3(.65f,-.32f,-.69f),
                new Vector3(.03f,-.91f,.43f)
            };
            foreach(Vector3 spot in spots)
            {
                Vector3 up=spot.normalized;
                GameObject rock=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rock.name="IMPASSABLE MOUNTAIN";rock.transform.SetParent(transform,false);
                rock.transform.position=Point(up,1.25f);
                rock.transform.rotation=Quaternion.FromToRotation(Vector3.up,up);
                rock.transform.localScale=new Vector3(1.15f,1.6f,1.15f);
                rock.GetComponent<Renderer>().sharedMaterial=Colors.New(new Color(.48f,.50f,.61f));
                rock.layer=8;
            }
        }
    }
    public static class Colors
    {
        public static Material FromResource(string assetName)
        {
            Material template = Resources.Load<Material>(assetName);
            if (template == null)
                throw new System.InvalidOperationException("Unity material missing from Resources: " + assetName);
            return new Material(template);
        }
        public static Material New(Color color, bool glow=false)
        {
            var reference=Resources.Load<Material>("PlanetStandard");
            if(reference==null)throw new System.InvalidOperationException("PlanetStandard material missing from Resources");
            var material=new Material(reference);
            material.color=color;material.SetFloat("_Glossiness",.42f);
            if(glow){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",color*.65f);}
            return material;
        }
        public static Color Team(int index)
        {
            switch(index){case 1:return new Color(1f,.25f,.60f);case 2:return new Color(.1f,.65f,1f);
              case 3:return new Color(1f,.64f,.16f);default:return new Color(.43f,.92f,.48f);}
        }
    }
}
