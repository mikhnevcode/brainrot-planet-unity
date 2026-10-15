using UnityEngine;
namespace BrainrotPlanet
{
    // Ten visible destinations correspond to the actual battle maps.
    // Pure decoration: no collider, rigidbody, navigation or random-state changes.
    public sealed class SolarSystemArt : MonoBehaviour
    {
        private Vector3 normal,up,sun;
        private float orbitStart;
        private OrbitCamera flightCamera;
        private Renderer[] flightArt;
        private bool approachCleared;
        private readonly Transform[] planets=new Transform[PlanetBiomeRules.Count];
        private readonly LineRenderer[] rings=new LineRenderer[PlanetBiomeRules.Count];
        public void Build(Vector3 opening,Vector3 screenUp,int selected,OrbitCamera camera)
        {
            normal=opening;up=screenUp;sun=DeploymentFlight.Sun(normal,up,selected);
            flightCamera=camera;
            orbitStart=Time.time;
            Stars(selected);
            Sphere("Sun",sun,16,new Color(1,.59f,.06f),new Color(1,.96f,.59f));
            for(int i=0;i<PlanetBiomeRules.Count;i++)
            {
                float radius=DeploymentFlight.OrbitRadius(i);
                Color accent=PlanetBiomeRules.OrbitAccent(i);
                rings[i]=Ring("Orbit "+i,sun,radius,Color.Lerp(accent,new Color(.22f,.35f,.55f),.55f),.14f);
                if(i==selected)continue; // The walkable world at origin is this orbit's destination.
                var rules=new PlanetBiomeRules((BattlePlanet)i,13007+i*19);
                Color a=rules.Ground(Vector3.up,.5f,.15f),b=rules.Ground(Vector3.up,.5f,.88f);
                planets[i]=Sphere("Orbiting "+(BattlePlanet)i,Orbit(i,0),i==2?7:9+(i%4)*1.2f,a,b,i,rules);
                if(i==6||i==8)
                {
                    var belt=Ring("Planetary belt "+i,planets[i].position,16,accent,.6f);
                    belt.useWorldSpace=false;
                    for(int j=0;j<belt.positionCount;j++)belt.SetPosition(j,belt.GetPosition(j)-planets[i].position);
                    belt.transform.SetParent(planets[i],false);
                }
            }
            flightArt=GetComponentsInChildren<Renderer>();
            Debug.Log("SOLAR_SYSTEM_READY planets="+PlanetBiomeRules.Count+" selected="+selected+" physics=false");
        }
        private static float Next(ref uint seed)
        {seed=seed*1664525u+1013904223u;return ((seed>>8)&0xffffff)/16777215f;}
        private void Stars(int selected)
        {
            const int count=300;
            var vertices=new Vector3[count*4];var colors=new Color[count*4];var triangles=new int[count*6];
            Vector3 right=Vector3.Cross(up,normal).normalized;
            uint seed=(uint)(13007+selected*1711);
            for(int i=0;i<count;i++)
            {
                float depth=180+Next(ref seed)*350;
                Vector3 center=sun+normal*depth+right*((Next(ref seed)*2-1)*depth*.26f)+
                    up*((Next(ref seed)*2-1)*depth*.50f);
                float size=.045f+Next(ref seed)*.12f;
                Color tint=Color.Lerp(new Color(.36f,.64f,1f),new Color(1f,.85f,.55f),Next(ref seed));
                int k=i*4,t=i*6;
                vertices[k]=center-right*size-up*size;vertices[k+1]=center+right*size-up*size;
                vertices[k+2]=center-right*size+up*size;vertices[k+3]=center+right*size+up*size;
                for(int j=0;j<4;j++)colors[k+j]=tint;
                triangles[t]=k;triangles[t+1]=k+1;triangles[t+2]=k+2;
                triangles[t+3]=k+1;triangles[t+4]=k+3;triangles[t+5]=k+2;
            }
            var mesh=new Mesh{name="Solar approach starfield"};mesh.vertices=vertices;mesh.colors=colors;
            mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
            var go=new GameObject("Flying through colorful stars");go.transform.SetParent(transform,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var material=new Material(Resources.Load<Shader>("PlanetBiomeDecor"));go.AddComponent<MeshRenderer>().sharedMaterial=material;
            go.AddComponent<PlanetGeneratedArt>().Own(mesh,material);
        }
        private Vector3 Orbit(int i,float seconds)
        {
            return sun+DeploymentFlight.OrbitOffset(normal,up,i,seconds);
        }
        private void Update()
        {
            // The system is an opening vista. Clear decorative bodies before
            // the flight corridor crosses an inner orbit; the selected planet
            // itself stays visible and is never part of this art hierarchy.
            if(!approachCleared&&flightCamera!=null&&flightCamera.DescentSeconds>=2.3f)
            {
                approachCleared=true;
                foreach(var renderer in flightArt)renderer.enabled=false;
            }
            if(approachCleared)return;
            for(int i=0;i<planets.Length;i++)if(planets[i]!=null)planets[i].position=Orbit(i,Time.time-orbitStart);
            bool showOrbits=flightCamera!=null&&flightCamera.IsDescending&&flightCamera.DescentSeconds<2.7f;
            for(int i=0;i<rings.Length;i++)if(rings[i]!=null)rings[i].enabled=showOrbits;
        }
        private LineRenderer Ring(string name,Vector3 center,float radius,Color color,float width)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);
            var line=go.AddComponent<LineRenderer>();line.positionCount=65;line.useWorldSpace=true;line.loop=false;
            var material=Colors.New(color,true);line.sharedMaterial=material;
            line.startWidth=line.endWidth=width;line.startColor=line.endColor=color;
            Vector3 right=Vector3.Cross(up,normal).normalized;
            for(int j=0;j<=64;j++){float a=j/64f*Mathf.PI*2;line.SetPosition(j,center+right*(Mathf.Cos(a)*radius)+normal*(Mathf.Sin(a)*radius));}
            go.AddComponent<PlanetGeneratedArt>().Own(null,material);
            return line;
        }
        private Transform Sphere(string name,Vector3 position,float radius,Color a,Color b,int planet=-1,PlanetBiomeRules rules=null)
        {
            const int lon=32,lat=18;
            var vertices=new Vector3[(lon+1)*(lat+1)];var normals=new Vector3[vertices.Length];var colors=new Color[vertices.Length];var triangles=new int[lon*lat*6];
            for(int y=0;y<=lat;y++)for(int x=0;x<=lon;x++)
            {
                int k=y*(lon+1)+x;Vector3 n=PlanetMath.FromLonLat(x/(float)lon,y/(float)lat);
                float altitude=rules!=null?rules.Terrain.Elevation(n):0;
                vertices[k]=n*radius*(1+altitude/PlanetMath.Radius);normals[k]=n;
                float detail=Mathf.PerlinNoise(n.x*5+9,n.z*5+n.y*2+4);
                if(planet==7||planet==8)detail=.5f+.5f*Mathf.Sin(n.y*27+n.x*3);
                if(planet==3)detail=Mathf.Lerp(detail,1,Mathf.Abs(n.y)*Mathf.Abs(n.y));
                colors[k]=rules!=null?rules.Ground(n,altitude,detail):Color.Lerp(a,b,Mathf.SmoothStep(0,1,(detail-.30f)*2.5f));
            }
            int index=0;for(int y=0;y<lat;y++)for(int x=0;x<lon;x++)
            {int k=y*(lon+1)+x,c=k+lon+1;triangles[index++]=k;triangles[index++]=c;triangles[index++]=k+1;triangles[index++]=k+1;triangles[index++]=c;triangles[index++]=c+1;}
            var mesh=new Mesh{name=name+" painted sphere"};mesh.vertices=vertices;mesh.normals=normals;mesh.colors=colors;mesh.triangles=triangles;mesh.RecalculateBounds();
            var go=new GameObject(name);go.transform.SetParent(transform,false);go.transform.position=position;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var material=new Material(Resources.Load<Shader>("PlanetBiomeDecor"));go.AddComponent<MeshRenderer>().sharedMaterial=material;
            go.AddComponent<PlanetGeneratedArt>().Own(mesh,material);return go.transform;
        }
    }
}
