using UnityEngine;

namespace BrainrotPlanet
{
    public sealed class PlanetSurface : MonoBehaviour
    {
        public MeshCollider GroundCollider { get; private set; }
        public Texture2D MapTexture { get; private set; }
        private const int VisualScale = 2;
        private const int VisualWidth = PlanetMath.Width * VisualScale;
        private const int VisualHeight = PlanetMath.Height * VisualScale;
        private Color32[] pixels;
        private Color32[] unpainted;
        private Color32[] sourcePixels;
        private Color32[] softenedPixels;
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
            MapTexture=new Texture2D(VisualWidth,VisualHeight,TextureFormat.RGBA32,false);
            MapTexture.wrapModeU=TextureWrapMode.Repeat;MapTexture.wrapModeV=TextureWrapMode.Clamp;
            MapTexture.filterMode=FilterMode.Bilinear;
            pixels=new Color32[VisualWidth*VisualHeight];
            unpainted=new Color32[PlanetMath.CellCount];
            sourcePixels=new Color32[PlanetMath.CellCount];
            softenedPixels=new Color32[PlanetMath.CellCount];
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
        /// <summary>
        /// Gameplay remains on a 256x128 ownership grid. The map draws a smooth,
        /// 2x-resolution visual representation; actual capture logic is unchanged.
        /// Trail display is owned by the smooth 3D LineRenderer, not cell squares.
        /// </summary>
        public void PaintTexture(byte[] owners, System.Collections.Generic.HashSet<int>[] trails)
        {
            float started=Time.realtimeSinceStartup;
            int w=PlanetMath.Width,h=PlanetMath.Height;
            for(int cell=0;cell<sourcePixels.Length;cell++)
            {
                byte owner=owners==null ? (byte)0 : owners[cell];
                sourcePixels[cell]=owner>0 && owner<teamColors.Length
                    ? teamColors[owner] : unpainted[cell];
            }

            // A lightweight 3x3 smoothing pass affects visual colors only.
            // Extra border softness rounds diagonal/corner steps in paint.
            for(int y=0;y<h;y++)
            {
                int up=Mathf.Max(0,y-1)*w;
                int current=y*w;
                int down=Mathf.Min(h-1,y+1)*w;
                for(int x=0;x<w;x++)
                {
                    int left=(x+w-1)%w,right=(x+1)%w;
                    int k=current+x;
                    Color32 c=sourcePixels[k];
                    Color32 a=sourcePixels[up+left],b=sourcePixels[up+x],
                        d=sourcePixels[up+right],e=sourcePixels[current+left],
                        f=sourcePixels[current+right],g=sourcePixels[down+left],
                        i=sourcePixels[down+x],j=sourcePixels[down+right];
                    // Kernel [1 2 1; 2 4 2; 1 2 1].
                    int rr=(a.r+2*b.r+d.r+2*e.r+4*c.r+2*f.r+g.r+2*i.r+j.r+8)/16;
                    int gg=(a.g+2*b.g+d.g+2*e.g+4*c.g+2*f.g+g.g+2*i.g+j.g+8)/16;
                    int bb=(a.b+2*b.b+d.b+2*e.b+4*c.b+2*f.b+g.b+2*i.b+j.b+8)/16;
                    // Retain full saturated interiors and only round edges.
                    softenedPixels[k]=new Color32(
                        (byte)((2*c.r+rr+1)/3),
                        (byte)((2*c.g+gg+1)/3),
                        (byte)((2*c.b+bb+1)/3),255);
                }
            }

            // 2x bilinear supersampling. Fixed 1:3 weights avoid float math
            // inside the pixel loop and wrap properly at the UV longitude seam.
            for(int y=0;y<VisualHeight;y++)
            {
                int iy=y/2;
                int y0=(y&1)==0 ? Mathf.Max(iy-1,0) : iy;
                int y1=(y&1)==0 ? iy : Mathf.Min(iy+1,h-1);
                int wy0=(y&1)==0 ? 1 : 3,wy1=4-wy0;
                int row0=y0*w,row1=y1*w,outrow=y*VisualWidth;
                for(int x=0;x<VisualWidth;x++)
                {
                    int ix=x/2;
                    int x0=(x&1)==0 ? (ix+w-1)%w : ix;
                    int x1=(x&1)==0 ? ix : (ix+1)%w;
                    int wx0=(x&1)==0 ? 1 : 3,wx1=4-wx0;
                    int aa=wx0*wy0,bb=wx1*wy0,cc=wx0*wy1,dd=wx1*wy1;
                    Color32 a=softenedPixels[row0+x0],b=softenedPixels[row0+x1],
                            c=softenedPixels[row1+x0],d=softenedPixels[row1+x1];
                    pixels[outrow+x]=new Color32(
                        (byte)((aa*a.r+bb*b.r+cc*c.r+dd*d.r+8)/16),
                        (byte)((aa*a.g+bb*b.g+cc*c.g+dd*d.g+8)/16),
                        (byte)((aa*a.b+bb*b.b+cc*c.b+dd*d.b+8)/16),255);
                }
            }
            MapTexture.SetPixels32(pixels);
            MapTexture.Apply(false);
            if(Debug.isDebugBuild)
                Debug.Log("PAINT_VISUAL_UPDATE resolution="+VisualWidth+"x"+VisualHeight+
                    " ms="+((Time.realtimeSinceStartup-started)*1000f).ToString("F2"));
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