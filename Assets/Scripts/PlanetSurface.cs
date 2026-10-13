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
        private float[] reliefTone;
        private readonly Color32[] teamColors = {
            new Color32(0,0,0,0),new Color32(255,78,169,255),
            new Color32(48,174,255,255),new Color32(255,174,57,255),
            new Color32(109,241,130,255)
        };
        private static readonly Vector3[] hills = {
            new Vector3(-.55f,.2f,.81f).normalized,
            new Vector3(.62f,-.30f,-.72f).normalized,
            new Vector3(-.04f,-.93f,.36f).normalized,
            new Vector3(-.88f,.36f,-.3f).normalized,
            new Vector3(.27f,.82f,-.5f).normalized,
            new Vector3(.72f,.55f,.41f).normalized,
            new Vector3(-.35f,-.58f,.74f).normalized
        };
        private static readonly float[] hillRadii={.19f,.17f,.22f,.16f,.20f,.15f,.18f};
        private static readonly float[] hillHeights={2.35f,1.85f,2.65f,1.70f,2.20f,1.45f,1.95f};
        private static readonly Vector3[] craters = {
            new Vector3(-.72f,.48f,.50f).normalized,
            // One early readable crater sits outside the landing core, while
            // the others remain distributed around the rest of the planet.
            new Vector3(.935f,-.11f,.34f).normalized,
            new Vector3(.73f,.54f,-.42f).normalized,
            new Vector3(-.20f,-.72f,-.66f).normalized,
            new Vector3(.47f,-.76f,.44f).normalized,
            new Vector3(-.92f,-.12f,-.37f).normalized
        };
        private static readonly float[] craterRadii={.16f,.14f,.18f,.14f,.13f,.17f};
        private static readonly float[] craterDepths={.72f,.65f,.82f,.58f,.52f,.76f};
        private static readonly float[] craterRims={.46f,.42f,.51f,.39f,.36f,.48f};

        public static int HillCount=>hills.Length;
        public static int CraterCount=>craters.Length;
        public static Vector3 HillDirection(int index)=>hills[index];
        public static float HillRadius(int index)=>hillRadii[index];
        public static Vector3 CraterDirection(int index)=>craters[index];
        public static float CraterRadius(int index)=>craterRadii[index];
        public static bool IsHillArea(Vector3 direction)
        {
            Vector3 n=direction.normalized;
            for(int i=0;i<hills.Length;i++)
                if(Vector3.Dot(n,hills[i])>=Mathf.Cos(hillRadii[i]*1.05f))
                    return true;
            return false;
        }

        private static float CapWeight(float dot,float radius)
        {
            float edge=Mathf.Cos(radius);
            float t=Mathf.Clamp01((dot-edge)/(1f-edge));
            return t*t*(3f-2f*t);
        }

        public static float Elevation(Vector3 direction)
        {
            Vector3 n = direction.normalized;
            float a=Mathf.PerlinNoise(n.x*5.2f+14.8f,n.z*5.2f+n.y*1.7f+3.1f);
            float b=Mathf.PerlinNoise(n.y*11f+9.2f,n.x*11f+n.z*4.3f+7.3f);
            float c=Mathf.PerlinNoise(n.z*23f+5.4f,n.y*19f+n.x*7f+11.7f);
            float height=.09f+.43f*a*a+.15f*b+.055f*c;
            for(int i=0;i<hills.Length;i++)
            {
                float hill=CapWeight(Vector3.Dot(n,hills[i]),hillRadii[i]);
                // Rounded standalone hills: no chains or linear ridges.
                height+=hillHeights[i]*hill*hill*(.88f+.12f*b);
            }
            for(int i=0;i<craters.Length;i++)
            {
                float edge=1f-Mathf.Cos(craterRadii[i]);
                float radial=(1f-Vector3.Dot(n,craters[i]))/edge;
                float bowl=Mathf.Clamp01(1f-radial);
                bowl=bowl*bowl*(3f-2f*bowl);
                float rim=Mathf.Clamp01(1f-Mathf.Abs(radial-1f)/.30f);
                rim=rim*rim*(3f-2f*rim);
                height-=craterDepths[i]*bowl;
                height+=craterRims[i]*rim;
            }
            return Mathf.Max(.035f,height);
        }
        public static Vector3 Point(Vector3 direction, float extra = 0)
        {
            Vector3 n = direction.normalized;
            return n * (PlanetMath.Radius + Elevation(n) + extra);
        }
        public void Build()
        {
            gameObject.layer = 8;
            const int lon = 224, lat = 112;
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
            mesh.RecalculateNormals();
            WeldVisualNormals(mesh,lon,lat);
            mesh.RecalculateBounds();
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            GroundCollider=gameObject.AddComponent<MeshCollider>();GroundCollider.sharedMesh=mesh;
            MapTexture=new Texture2D(VisualWidth,VisualHeight,TextureFormat.RGBA32,false);
            MapTexture.wrapModeU=TextureWrapMode.Repeat;MapTexture.wrapModeV=TextureWrapMode.Clamp;
            MapTexture.filterMode=FilterMode.Bilinear;
            pixels=new Color32[VisualWidth*VisualHeight];
            unpainted=new Color32[PlanetMath.CellCount];
            sourcePixels=new Color32[PlanetMath.CellCount];
            softenedPixels=new Color32[PlanetMath.CellCount];
            reliefTone=new float[PlanetMath.CellCount];
            PrepareBaseColors();
            var mat = Colors.New(Color.white);mat.name="Planet paint / relief";
            mat.SetFloat("_Glossiness",.18f);mat.SetTexture("_MainTex",MapTexture);
            gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;
            PaintTexture(null,null);
            Debug.Log("PLANET_RELIEF_READY vertices="+v.Length+
                " hills="+hills.Length+" craters="+craters.Length+
                " primitiveProps=0");
        }
        private static void WeldVisualNormals(Mesh mesh,int lon,int lat)
        {
            Vector3[] normals=mesh.normals;
            int stride=lon+1;
            for(int j=1;j<lat;j++)
            {
                int left=j*stride,right=left+lon;
                Vector3 joined=(normals[left]+normals[right]).normalized;
                normals[left]=joined;normals[right]=joined;
            }
            Vector3 north=Vector3.zero,south=Vector3.zero;
            for(int i=0;i<=lon;i++)
            {
                north+=normals[i];
                south+=normals[lat*stride+i];
            }
            north.Normalize();south.Normalize();
            for(int i=0;i<=lon;i++)
            {
                normals[i]=north;
                normals[lat*stride+i]=south;
            }
            mesh.normals=normals;
        }
        private void PrepareBaseColors()
        {
            for(int cell=0;cell<unpainted.Length;cell++)
            {
                Vector3 n=PlanetMath.CellDirection(cell);
                float noise=Mathf.PerlinNoise(n.x*7f+3.4f,n.z*7f+n.y*5f+1.6f);
                float alt=Elevation(n);
                Color baseColor=alt<.11f ? new Color(.35f,.43f,.43f) :
                    alt>1.95f ? new Color(.57f,.58f,.64f) :
                    noise>.61f ? new Color(.44f,.75f,.63f) :
                    noise<.34f ? new Color(.73f,.76f,.58f) :
                    new Color(.64f,.80f,.69f);
                if(alt>2.75f)baseColor=new Color(.78f,.80f,.85f);
                float grain=(Mathf.PerlinNoise(n.x*31f+8.1f,n.z*29f+n.y*13f)-.5f)*.10f;
                baseColor+=new Color(grain,grain,grain,0f);
                reliefTone[cell]=ReliefTone(n,noise);
                unpainted[cell]=Tone(baseColor,reliefTone[cell]);
            }
        }
        private static float ReliefTone(Vector3 n,float noise)
        {
            float tone=.96f+(noise-.5f)*.14f;
            for(int i=0;i<hills.Length;i++)
            {
                float hill=CapWeight(Vector3.Dot(n,hills[i]),hillRadii[i]);
                // Brighter crown and a slightly darker foot make the rounded
                // silhouette readable even below brightly painted territory.
                tone*=1f-.09f*Mathf.Sin(hill*Mathf.PI)+.18f*hill*hill;
            }
            for(int i=0;i<craters.Length;i++)
            {
                float edge=1f-Mathf.Cos(craterRadii[i]);
                float radial=(1f-Vector3.Dot(n,craters[i]))/edge;
                float bowl=Mathf.Clamp01(1f-radial);
                bowl=bowl*bowl*(3f-2f*bowl);
                float rim=Mathf.Clamp01(1f-Mathf.Abs(radial-1f)/.30f);
                rim=rim*rim*(3f-2f*rim);
                tone*=1f-.38f*bowl+.24f*rim;
            }
            return Mathf.Clamp(tone,.55f,1.28f);
        }
        private static Color32 Tone(Color color,float tone)
        {
            color*=tone;
            color.a=1f;
            return color;
        }
        private static Color32 Tone(Color32 color,float tone)
        {
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.r*tone),0,255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.g*tone),0,255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.b*tone),0,255),255);
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
                    ? Tone(teamColors[owner],reliefTone[cell]) : unpainted[cell];
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
