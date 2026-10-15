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
        private byte[] ownerWeights;
        private byte[] uniformOwner;
        private byte[] strokeOwner;
        private byte[] strokeCoverage;
        private bool hasStrokes;
        private float[] reliefTone;
        private PlanetBiomeRules biome;
        private static PlanetSurface activeSurface;
        public PlanetTerrainProfile Terrain {get;private set;}
        private static PlanetTerrainProfile ActiveTerrain=>activeSurface!=null?activeSurface.Terrain:null;
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

        public static int HillCount=>ActiveTerrain?.Mounds.Length??hills.Length;
        public static int CraterCount=>ActiveTerrain?.Bowls.Length??craters.Length;
        public static Vector3 HillDirection(int index)=>ActiveTerrain!=null?ActiveTerrain.Mounds[index].Direction:hills[index];
        public static float HillRadius(int index)=>ActiveTerrain!=null?ActiveTerrain.Mounds[index].Radius:hillRadii[index];
        public static Vector3 CraterDirection(int index)=>ActiveTerrain!=null?ActiveTerrain.Bowls[index].Direction:craters[index];
        public static float CraterRadius(int index)=>ActiveTerrain!=null?ActiveTerrain.Bowls[index].Radius:craterRadii[index];
        public static bool IsHillArea(Vector3 direction)
        {
            Vector3 n=direction.normalized;
            if(ActiveTerrain!=null)return ActiveTerrain.IsHill(n);
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
            if(ActiveTerrain!=null)return ActiveTerrain.Elevation(n);
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
        public static Vector3 Point(Vector3 direction,float extra,PlanetTerrainProfile terrain)
        {Vector3 n=direction.normalized;return n*(PlanetMath.Radius+terrain.Elevation(n)+extra);}
        public void AddCapturedStroke(int team,
            System.Collections.Generic.IReadOnlyList<Vector3> path,Vector3 closure)
        {
            if(Application.isBatchMode||team<1||team>4||path==null||path.Count<2)return;
            BakeCapturedStroke(team,path,closure);
            CapturedPaintStroke.Create(transform,team,path,closure);
        }
        public int StrokePixelCount
        {
            get
            {
                if(strokeOwner==null)return 0;
                int count=0;
                for(int i=0;i<strokeOwner.Length;i++)if(strokeOwner[i]!=0)count++;
                return count;
            }
        }
        private void BakeCapturedStroke(int team,
            System.Collections.Generic.IReadOnlyList<Vector3> path,Vector3 closure)
        {
            Vector3 first=path[0].normalized,second=path[1].normalized;
            Vector3 start=ExtendStroke(first,first-second);
            StampStrokeSegment(team,start,first);
            Vector3 previous=first;
            int stride=Mathf.Max(1,(path.Count-1)/350);
            for(int i=stride;i<path.Count;i+=stride)
            {
                Vector3 current=path[i].normalized;
                StampStrokeSegment(team,previous,current);previous=current;
            }
            Vector3 last=path[path.Count-1].normalized;
            StampStrokeSegment(team,previous,last);
            Vector3 end=closure.sqrMagnitude>.5f&&Vector3.Angle(last,closure)<8f?
                closure.normalized:ExtendStroke(last,last-path[path.Count-2]);
            StampStrokeSegment(team,last,end);
            hasStrokes=true;
        }
        private static Vector3 ExtendStroke(Vector3 at,Vector3 tangent)
        {
            Vector3 along=Vector3.ProjectOnPlane(tangent,at);
            return along.sqrMagnitude<.000001f?at:(at+along.normalized*.022f).normalized;
        }
        private void StampStrokeSegment(int team,Vector3 from,Vector3 to)
        {
            int steps=Mathf.Clamp(Mathf.CeilToInt(Vector3.Angle(from,to)*Mathf.Deg2Rad/.004f),1,36);
            for(int step=0;step<=steps;step++)
                StampStrokePoint(team,Vector3.Slerp(from,to,(float)step/steps).normalized);
        }
        private void StampStrokePoint(int team,Vector3 n)
        {
            Vector2 uv=PlanetMath.ToUV(n);
            float cx=uv.x*VisualWidth,cy=uv.y*VisualHeight;
            const float outer=.72f,inner=.39f;
            float metresY=Mathf.PI*PlanetMath.Radius/VisualHeight;
            int rangeY=Mathf.CeilToInt(outer/metresY)+1;
            // Longitude has no meaning at a pole. Sample the small polar cap in
            // world angles so a geodesic stroke cannot collapse into one spoke.
            float poleAngle=Mathf.Acos(Mathf.Clamp(Mathf.Abs(n.y),0,1));
            if(poleAngle<outer/PlanetMath.Radius+2f*Mathf.PI/VisualHeight)
            {
                StampPolarStrokePoint(team,n,cy,rangeY,outer,inner);return;
            }
            float metresX=2f*Mathf.PI*PlanetMath.Radius*
                Mathf.Max(.08f,Mathf.Sqrt(Mathf.Max(0f,1f-n.y*n.y)))/VisualWidth;
            int rangeX=Mathf.Min(12,Mathf.CeilToInt(outer/metresX)+1);
            int centerX=Mathf.FloorToInt(cx),centerY=Mathf.FloorToInt(cy);
            for(int dy=-rangeY;dy<=rangeY;dy++)
            {
                int y=centerY+dy;
                if(y<0||y>=VisualHeight)continue;
                float py=(y+.5f-cy)*metresY;
                for(int dx=-rangeX;dx<=rangeX;dx++)
                {
                    int x=centerX+dx;
                    float px=(x+.5f-cx)*metresX;
                    float distance=Mathf.Sqrt(px*px+py*py);
                    if(distance>=outer)continue;
                    byte coverage=(byte)Mathf.RoundToInt(255f*Mathf.Clamp01((outer-distance)/(outer-inner)));
                    int wrapped=(x+VisualWidth)%VisualWidth;
                    int pixel=y*VisualWidth+wrapped;
                    if(strokeOwner[pixel]!=team||coverage>strokeCoverage[pixel])
                    {strokeOwner[pixel]=(byte)team;strokeCoverage[pixel]=coverage;}
                }
            }
        }
        private void StampPolarStrokePoint(int team,Vector3 n,float cy,int rangeY,float outer,float inner)
        {
            int centerY=Mathf.FloorToInt(cy);
            float cosOuter=Mathf.Cos(outer/PlanetMath.Radius);
            for(int dy=-rangeY;dy<=rangeY;dy++)
            {
                int y=centerY+dy;if(y<0||y>=VisualHeight)continue;
                float v=(y+.5f)/VisualHeight;
                int row=y*VisualWidth;
                for(int x=0;x<VisualWidth;x++)
                {
                    Vector3 sample=PlanetMath.FromLonLat((x+.5f)/VisualWidth,v);
                    float dot=Vector3.Dot(n,sample);if(dot<=cosOuter)continue;
                    float distance=Mathf.Acos(Mathf.Clamp(dot,-1,1))*PlanetMath.Radius;
                    byte coverage=(byte)Mathf.RoundToInt(255f*Mathf.Clamp01((outer-distance)/(outer-inner)));
                    int pixel=row+x;
                    if(strokeOwner[pixel]!=team||coverage>strokeCoverage[pixel])
                    {strokeOwner[pixel]=(byte)team;strokeCoverage[pixel]=coverage;}
                }
            }
        }
        public void Build(PlanetBiomeRules rules=null)
        {
            biome=rules;
            Terrain=rules?.Terrain;
            // One arena per process. Destruction of an older replay surface must
            // never unbind the newer one from terrain-following actors and paint.
            activeSurface=this;
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
            ownerWeights=new byte[PlanetMath.CellCount*5];
            uniformOwner=new byte[PlanetMath.CellCount];
            strokeOwner=new byte[VisualWidth*VisualHeight];
            strokeCoverage=new byte[VisualWidth*VisualHeight];
            hasStrokes=false;
            reliefTone=new float[PlanetMath.CellCount];
            PrepareBaseColors();
            var shader=Resources.Load<Shader>("PlanetPaint");
            var mat=shader!=null?new Material(shader):Colors.New(Color.white);
            mat.name="Planet paint / fine continuous grain";
            mat.SetTexture("_MainTex",MapTexture);
            if(shader!=null)mat.SetTexture("_DetailTex",rules!=null?PlanetBiomeGrain.Get(rules.Kind):Resources.Load<Texture2D>("PlanetGrain064"));
            gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;
            PaintTexture(null,null);
            Debug.Log("PLANET_RELIEF_READY vertices="+v.Length+
                " hills="+HillCount+" craters="+CraterCount+
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
        private void OnDestroy()
        {
            if(activeSurface==this)activeSurface=null;
            // Runtime-generated resources are not assets owned by the scene.
            // Release them explicitly when replay destroys the old planet.
            if(MapTexture!=null)Destroy(MapTexture);
            var filter=GetComponent<MeshFilter>();if(filter!=null&&filter.sharedMesh!=null)Destroy(filter.sharedMesh);
            var renderer=GetComponent<Renderer>();if(renderer!=null&&renderer.sharedMaterial!=null)Destroy(renderer.sharedMaterial);
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
                if(biome!=null)baseColor=biome.Ground(n,alt,noise);
                float grain=(Mathf.PerlinNoise(n.x*31f+8.1f,n.z*29f+n.y*13f)-.5f)*.10f;
                baseColor+=new Color(grain,grain,grain,0f);
                reliefTone[cell]=ReliefTone(n,noise);
                unpainted[cell]=Tone(baseColor,reliefTone[cell]);
            }
        }
        private static float ReliefTone(Vector3 n,float noise)
        {
            if(ActiveTerrain!=null)return ActiveTerrain.ReliefTone(n,noise);
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
        /// Gameplay remains on a 256x128 ownership grid. Only the visual map is
        /// 2x larger. A smoothed ownership field shapes the visual contour;
        /// capture continues to use the original, unsmoothed ownership grid.
        /// </summary>
        public void PaintTexture(byte[] owners, System.Collections.Generic.HashSet<int>[] trails)
        {
            float started=Time.realtimeSinceStartup;
            int w=PlanetMath.Width,h=PlanetMath.Height;
            // The center keeps a single captured cell visible, while the
            // surrounding votes round convex and concave grid corners.
            System.Array.Clear(ownerWeights,0,ownerWeights.Length);
            for(int y=0;y<h;y++)
            {
                int row=y*w;
                for(int x=0;x<w;x++)
                {
                    int weightOffset=(row+x)*5;
                    for(int dy=-1;dy<=1;dy++)
                    {
                        int neighborY=Mathf.Clamp(y+dy,0,h-1),neighborRow=neighborY*w;
                        for(int dx=-1;dx<=1;dx++)
                        {
                            int neighbor=PlanetMath.CanonicalCell(neighborRow+(x+dx+w)%w);
                            byte neighborOwner=owners==null?(byte)0:owners[neighbor];
                            if(neighborOwner>=5)neighborOwner=0;
                            int vote=dx==0&&dy==0?10:dx==0||dy==0?2:1;
                            ownerWeights[weightOffset+neighborOwner]+=(byte)vote;
                        }
                    }
                    int best=0,bestScore=-1;
                    for(int team=0;team<5;team++)
                    {
                        int score=ownerWeights[weightOffset+team];
                        if(score>bestScore)
                        {bestScore=score;best=team;}
                    }
                    int cell=row+x;
                    byte owner=owners==null?(byte)0:owners[PlanetMath.CanonicalCell(cell)];
                    sourcePixels[cell]=owner>0&&owner<5?
                        Tone(teamColors[owner],reliefTone[cell]):unpainted[cell];
                    uniformOwner[cell]=bestScore==22?(byte)best:(byte)255;
                }
            }

            // Interiors take the fast color path. Only pixels whose four
            // source cells touch a boundary reconstruct a smooth contour.
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
                    int i00=row0+x0,i10=row0+x1,i01=row1+x0,i11=row1+x1;
                    byte uniform=uniformOwner[i00];
                    if(uniform!=255 && uniformOwner[i10]==uniform &&
                       uniformOwner[i01]==uniform && uniformOwner[i11]==uniform)
                    {
                        Color32 a=sourcePixels[i00],b=sourcePixels[i10],
                                c=sourcePixels[i01],d=sourcePixels[i11];
                        pixels[outrow+x]=new Color32(
                            (byte)((aa*a.r+bb*b.r+cc*c.r+dd*d.r+8)/16),
                            (byte)((aa*a.g+bb*b.g+cc*c.g+dd*d.g+8)/16),
                            (byte)((aa*a.b+bb*b.b+cc*c.b+dd*d.b+8)/16),255);
                        continue;
                    }
                    int p00=i00*5,p10=i10*5,p01=i01*5,p11=i11*5;
                    int best=-1,second=-1,bestScore=-1,secondScore=-1;
                    for(int team=0;team<5;team++)
                    {
                        int score=aa*ownerWeights[p00+team]+bb*ownerWeights[p10+team]+
                            cc*ownerWeights[p01+team]+dd*ownerWeights[p11+team];
                        if(score>bestScore)
                        {secondScore=bestScore;second=best;bestScore=score;best=team;}
                        else if(score>secondScore)
                        {secondScore=score;second=team;}
                    }
                    int gap=bestScore-secondScore;
                    int mix=secondScore>0?Mathf.Clamp((160-gap)*128/160,0,128):0;
                    int cell=iy*w+ix;
                    Color32 first=best==0?unpainted[cell]:Tone(teamColors[best],reliefTone[cell]);
                    Color32 other=second==0?unpainted[cell]:Tone(teamColors[second],reliefTone[cell]);
                    int r=(first.r*(256-mix)+other.r*mix+128)/256;
                    int g=(first.g*(256-mix)+other.g*mix+128)/256;
                    int b2=(first.b*(256-mix)+other.b*mix+128)/256;
                    if(gap<176 && (best>0||second>0))
                    {
                        int rim=(176-gap)*96/176;
                        r=(r*(256-rim)+255*rim+128)/256;
                        g=(g*(256-rim)+255*rim+128)/256;
                        b2=(b2*(256-rim)+255*rim+128)/256;
                    }
                    pixels[outrow+x]=new Color32((byte)r,(byte)g,(byte)b2,255);
                }
            }
            // Every mesh vertex at v=0 or v=1 occupies the same physical
            // point. Keep those texture rows identical even when the adjacent
            // latitude ring contains several teams, otherwise bilinear
            // sampling turns the pole into longitude-shaped wedges.
            Color32 southPole=sourcePixels[0];
            int northCell=(h-1)*w,northRow=(VisualHeight-1)*VisualWidth;
            Color32 northPole=sourcePixels[northCell];
            for(int x=0;x<VisualWidth;x++)
            {
                pixels[x]=southPole;
                pixels[northRow+x]=northPole;
            }
            // Preserve the true vector trail as a smooth, lasting paint mark.
            // It never changes ownership, and a later takeover erases stale ink.
            if(hasStrokes&&owners!=null)
            {
                for(int y=0;y<VisualHeight;y++)
                {
                    int row=y*VisualWidth,cellRow=(y/2)*w;
                    for(int x=0;x<VisualWidth;x++)
                    {
                        int pixel=row+x,team=strokeOwner[pixel];
                        if(team==0)continue;
                        int cell=PlanetMath.CanonicalCell(cellRow+x/2);
                        if(owners[cell]!=team)
                        {strokeOwner[pixel]=0;strokeCoverage[pixel]=0;continue;}
                        Color32 target=Tone(teamColors[team],reliefTone[cell]);
                        int alpha=strokeCoverage[pixel]*225/255;
                        Color32 c=pixels[pixel];
                        pixels[pixel]=new Color32(
                            (byte)((c.r*(255-alpha)+target.r*alpha+127)/255),
                            (byte)((c.g*(255-alpha)+target.g*alpha+127)/255),
                            (byte)((c.b*(255-alpha)+target.b*alpha+127)/255),255);
                    }
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
        private static Material trailInk;
        public static Material TrailInkMaterial()
        {
            if(trailInk==null)
            {
                Shader shader=Resources.Load<Shader>("PlanetPaintStroke");
                if(shader==null)throw new System.InvalidOperationException("PlanetPaintStroke shader missing");
                trailInk=new Material(shader){name="Shared planet paint ink"};
            }
            return trailInk;
        }
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
            // Match the exact texture palette used by PlanetSurface.teamColors.
            switch(index){case 1:return new Color(1f,78f/255f,169f/255f);
              case 2:return new Color(48f/255f,174f/255f,1f);
              case 3:return new Color(1f,174f/255f,57f/255f);
              default:return new Color(109f/255f,241f/255f,130f/255f);}
        }
    }
}
