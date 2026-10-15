using System.Collections.Generic;
using UnityEngine;
namespace BrainrotPlanet
{
    // One bounded mesh of curved/faceted art. It never supplies collision.
    public sealed class PlanetBiomeDecor : MonoBehaviour
    {
        private Mesh mesh;private Material material;private PlanetBiomeRules rules;
        private readonly List<Vector3> vertices=new List<Vector3>();
        private readonly List<Color> colors=new List<Color>();
        private readonly List<Vector2> motion=new List<Vector2>();
        private readonly List<int> triangles=new List<int>();
        private uint random;
        private float Unit()=>PlanetBiomeRules.Unit(ref random);
        private Vector3 Point(Vector3 n,float height=.03f)=>PlanetSurface.Point(n,height,rules.Terrain);
        private void Vertex(Vector3 p,Color color,float sway=0,float glow=0)
        {vertices.Add(p);colors.Add(color);motion.Add(new Vector2(sway,glow));}
        private void Tri(Vector3 a,Vector3 b,Vector3 c,Color color,float glow=0)
        {
            int k=vertices.Count;Vertex(a,color,0,glow);Vertex(b,color,0,glow);Vertex(c,color,0,glow);
            triangles.Add(k);triangles.Add(k+1);triangles.Add(k+2);
        }
        private void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color,float sway=0,float glow=0)
        {
            int k=vertices.Count;Vertex(a,color,0,glow);Vertex(b,color,0,glow);Vertex(c,color,sway,glow);Vertex(d,color,sway,glow);
            triangles.Add(k);triangles.Add(k+2);triangles.Add(k+1);triangles.Add(k+1);triangles.Add(k+2);triangles.Add(k+3);
        }
        private void Tube(Vector3 from,Vector3 to,float radius,float endRadius,Color color)
        {
            Vector3 axis=(to-from).normalized;PlanetBiomeRules.Basis(axis,out Vector3 side,out Vector3 around);
            for(int j=0;j<6;j++)
            {
                float a=j*Mathf.PI/3,b=(j+1)*Mathf.PI/3;
                Vector3 p=side*Mathf.Cos(a)+around*Mathf.Sin(a),q=side*Mathf.Cos(b)+around*Mathf.Sin(b);
                Quad(from+p*radius,from+q*radius,to+p*endRadius,to+q*endRadius,Color.Lerp(color,Color.white,j%2*.06f));
            }
        }
        private void Leaf(Vector3 root,Vector3 up,Vector3 along,float length,float width,Color color)
        {
            Vector3 side=Vector3.Cross(up,along).normalized;
            for(int j=0;j<4;j++)
            {
                float a=j/4f,b=(j+1)/4f;
                Vector3 p=root+along*(length*a)+up*(length*(.46f*a-.60f*a*a));
                Vector3 q=root+along*(length*b)+up*(length*(.46f*b-.60f*b*b));
                float wa=width*Mathf.Sin(Mathf.PI*a),wb=width*Mathf.Sin(Mathf.PI*b);
                Quad(p-side*wa,p+side*wa,q-side*wb,q+side*wb,Color.Lerp(color,Color.white,b*.20f),b);
            }
        }
        private void Stone(Vector3 n,float radius,float height,Color color,bool pointed=false,float glow=0)
        {
            Vector3 root=Point(n,.02f);PlanetBiomeRules.Basis(n,out Vector3 east,out Vector3 north);
            for(int j=0;j<8;j++)
            {
                float a=j*Mathf.PI/4,b=(j+1)*Mathf.PI/4;
                Vector3 p=east*Mathf.Cos(a)+north*Mathf.Sin(a),q=east*Mathf.Cos(b)+north*Mathf.Sin(b);
                Vector3 shoulder=root+n*height*.52f;Color shade=Color.Lerp(color,Color.white,(j%3)*.07f);
                Quad(root+p*radius,root+q*radius,shoulder+p*radius*.78f,shoulder+q*radius*.78f,shade,0,glow);
                Tri(shoulder+p*radius*.78f,shoulder+q*radius*.78f,root+n*height,
                    Color.Lerp(shade,Color.white,pointed?.24f:.10f),glow);
            }
        }
        private void Crystal(Vector3 n,float height,float radius,Color color)
        {
            Vector3 root=Point(n);PlanetBiomeRules.Basis(n,out Vector3 east,out Vector3 north);
            for(int j=0;j<6;j++)
            {
                float a=j*Mathf.PI/3,b=(j+1)*Mathf.PI/3;
                Vector3 p=east*Mathf.Cos(a)+north*Mathf.Sin(a),q=east*Mathf.Cos(b)+north*Mathf.Sin(b);
                Color face=Color.Lerp(color,Color.white,(j%3)*.16f);Vector3 shoulder=root+n*height*.7f;
                Quad(root+p*radius,root+q*radius,shoulder+p*radius,shoulder+q*radius,face,0,.35f);
                Tri(shoulder+p*radius,shoulder+q*radius,root+n*height,Color.Lerp(face,Color.white,.25f),.5f);
            }
        }
        private void Stroke(Vector3 a,Vector3 b,float width,Color color,float glow=0)
        {
            Vector3 side=Vector3.Cross((a+b).normalized,b-a).normalized*width;
            Quad(Point((a-side).normalized,.04f),Point((a+side).normalized,.04f),
                Point((b-side).normalized,.04f),Point((b+side).normalized,.04f),color,0,glow);
        }
        private void Arc(Vector3 n,float radius,float phase,float span,Color color,float glow=0)
        {
            PlanetBiomeRules.Basis(n,out Vector3 east,out Vector3 north);
            Vector3 previous=(n+(east*Mathf.Cos(phase)+north*Mathf.Sin(phase))*radius).normalized;
            for(int j=1;j<=16;j++)
            {float a=phase+span*j/16;Vector3 next=(n+(east*Mathf.Cos(a)+north*Mathf.Sin(a))*radius).normalized;
                Stroke(previous,next,.0012f,color,glow);previous=next;}
        }
        private Vector3 Scatter(Vector3 n,Vector3 east,Vector3 north,float size=.105f)
        {float a=Unit()*Mathf.PI*2,r=Mathf.Sqrt(Unit())*size;return (n+(east*Mathf.Cos(a)+north*Mathf.Sin(a))*r).normalized;}
        private void Grass(Vector3 n,Vector3 east,Vector3 north,int count,Color dark,Color light)
        {
            for(int blade=0;blade<count;blade++)
            {
                Vector3 at=Scatter(n,east,north);float turn=Unit()*Mathf.PI*2;
                Vector3 along=east*Mathf.Cos(turn)+north*Mathf.Sin(turn),side=Vector3.Cross(at,along).normalized;
                Vector3 root=Point(at);float height=.28f+Unit()*.26f;Color color=Color.Lerp(dark,light,Unit());
                for(int j=0;j<3;j++)
                {
                    float a=j/3f,b=(j+1)/3f;Vector3 p=root+at*(height*a)+along*(height*.35f*a*a),q=root+at*(height*b)+along*(height*.35f*b*b);
                    Quad(p-side*(1-a)*.026f,p+side*(1-a)*.026f,q-side*(1-b)*.026f,q+side*(1-b)*.026f,color,b);
                }
            }
        }
        private void Flower(Vector3 n,Color color)
        {
            PlanetBiomeRules.Basis(n,out Vector3 east,out Vector3 north);Vector3 root=Point(n,.13f);
            for(int j=0;j<6;j++)
            {float a=j*Mathf.PI/3,b=(j+1)*Mathf.PI/3;
                Tri(root,root+(east*Mathf.Cos(a)+north*Mathf.Sin(a))*.15f,
                    root+(east*Mathf.Cos(b)+north*Mathf.Sin(b))*.15f,color);}
        }
        private void Wind(Vector3 n,Vector3 along,Vector3 across,Color color)
        {
            for(int arrow=0;arrow<3;arrow++)
            {Vector3 centre=(n+along*(arrow-1)*.065f).normalized;
                for(int sign=-1;sign<=1;sign+=2)
                    Stroke((centre+across*sign*.025f-along*.02f).normalized,(centre+along*.025f).normalized,.002f,color);}
        }
        public void Build(PlanetBiomeRules source)
        {
            rules=source;random=(uint)rules.Seed;
            int count=rules.Zones.Length>0?rules.Zones.Length:rules.Terrain.Bowls.Length;
            for(int zone=0;zone<count;zone++)
            {
                Vector3 n=rules.Zones.Length>0?rules.Zones[zone]:rules.Terrain.Bowls[zone].Direction;
                PlanetBiomeRules.Basis(n,out Vector3 east,out Vector3 north);
                switch(rules.Kind)
                {
                    case BattlePlanet.Tropical:
                        Grass(n,east,north,42,new Color(.10f,.42f,.20f),new Color(.60f,.84f,.25f));
                        for(int flower=0;flower<5;flower++)Flower(Scatter(n,east,north),flower%2==0?new Color(1,.82f,.38f):new Color(.99f,.58f,.78f));
                        if(zone%3==0)
                        {
                            Vector3 at=(n+east*.093f).normalized,root=Point(at),mid=root+at*.75f-east*.10f,top=root+at*1.45f-east*.22f;
                            Tube(root,mid,.10f,.08f,new Color(.51f,.34f,.20f));Tube(mid,top,.08f,.04f,new Color(.68f,.47f,.28f));
                            for(int leaf=0;leaf<7;leaf++){float a=leaf*Mathf.PI*2/7;Leaf(top,at,east*Mathf.Cos(a)+north*Mathf.Sin(a),.90f,.15f,new Color(.22f,.60f,.30f));}
                        }
                        break;
                    case BattlePlanet.Desert:
                        Wind(n,east,north,new Color(.97f,.86f,.64f));
                        for(int rock=0;rock<3;rock++)Stone(Scatter(n,east,north),.24f+Unit()*.17f,.18f+Unit()*.22f,new Color(.75f,.52f,.32f));
                        for(int stripe=0;stripe<3;stripe++)Arc(n,.052f+stripe*.018f,.5f,2.5f,new Color(.84f,.63f,.40f));
                        break;
                    case BattlePlanet.Moon:
                        for(int rock=0;rock<5;rock++)Stone(Scatter(n,east,north,.24f),.13f+Unit()*.12f,.10f+Unit()*.17f,new Color(.57f,.61f,.70f));
                        break;
                    case BattlePlanet.Frost:
                        for(int ice=0;ice<3;ice++)Crystal(Scatter(n,east,north),.36f+Unit()*.42f,.13f,new Color(.69f,.91f,1f));
                        for(int branch=0;branch<4;branch++)
                        {Vector3 tip=(n+(east*Mathf.Cos(branch*1.57f)+north*Mathf.Sin(branch*1.57f))*.07f).normalized;
                            Stroke(n,tip,.0013f,new Color(.82f,.98f,1f),.2f);}
                        break;
                    case BattlePlanet.Volcanic:
                        for(int vein=0;vein<7;vein++)
                        {
                            float a=vein*Mathf.PI*2/7;Vector3 dir=east*Mathf.Cos(a)+north*Mathf.Sin(a);
                            Vector3 p=(n+dir*.035f).normalized,q=(n+dir*.095f+east*.011f).normalized;
                            Stroke(n,p,.0014f,new Color(1f,.48f,.08f),1);Stroke(p,q,.0010f,new Color(1f,.68f,.17f),1);
                        }
                        for(int rock=0;rock<3;rock++)Stone(Scatter(n,east,north),.26f,.23f,new Color(.30f,.25f,.28f));
                        break;
                    case BattlePlanet.Oceanic:
                        Grass(n,east,north,26,new Color(.10f,.47f,.52f),new Color(.30f,.80f,.66f));
                        for(int coral=0;coral<3;coral++)
                        {
                            Vector3 at=Scatter(n,east,north),root=Point(at),top=root+at*.45f;
                            Color c=coral%2==0?new Color(.97f,.67f,.51f):new Color(.81f,.62f,.88f);
                            Tube(root,top,.043f,.035f,c);Tube(root+at*.19f,top+east*.22f,.035f,.015f,c);Tube(root+at*.15f,top-north*.18f,.035f,.015f,c);
                        }
                        Arc(n,.105f,.2f,2.3f,new Color(.80f,.96f,.91f));
                        break;
                    case BattlePlanet.Crystal:
                        for(int crystal=0;crystal<4;crystal++)Crystal(Scatter(n,east,north),.45f+Unit()*.58f,.11f+Unit()*.08f,
                            crystal%2==0?new Color(.62f,.70f,1f):new Color(.87f,.63f,.98f));
                        Arc(n,.080f,0,Mathf.PI*2,new Color(.74f,.78f,.97f),.4f);
                        break;
                    case BattlePlanet.Storm:
                        Wind(n,east,north,new Color(.69f,.83f,1f));
                        for(int fork=0;fork<3;fork++)
                        {Vector3 p=(n+east*(fork-1)*.065f-north*.05f).normalized;
                            for(int part=0;part<5;part++)
                            {Vector3 q=(p+north*.025f+east*((part%2==0?1:-1)*.012f)).normalized;Stroke(p,q,.0014f,new Color(.65f,.85f,1f),.85f);p=q;}}
                        break;
                    case BattlePlanet.Amber:
                        Wind(n,east,north,new Color(1f,.85f,.44f));
                        for(int nugget=0;nugget<4;nugget++)Stone(Scatter(n,east,north),.17f+Unit()*.15f,.18f+Unit()*.24f,
                            nugget%2==0?new Color(.88f,.47f,.15f):new Color(1f,.73f,.26f),true,.2f);
                        for(int leaf=0;leaf<4;leaf++)Leaf(Point(Scatter(n,east,north)),n,east,.26f,.065f,new Color(.71f,.44f,.17f));
                        break;
                    case BattlePlanet.Candy:
                        Arc(n,.07f,0,Mathf.PI*2,new Color(.99f,.85f,.60f));Arc(n,.09f,.5f,Mathf.PI*2,new Color(.93f,.71f,.87f));
                        for(int sweet=0;sweet<4;sweet++)Stone(Scatter(n,east,north),.22f,.28f,new Color(.86f,.65f,.80f),true);
                        for(int sprinkle=0;sprinkle<18;sprinkle++)
                        {Vector3 at=Scatter(n,east,north),dir=(east*Mathf.Cos(sprinkle)+north*Mathf.Sin(sprinkle))*.003f;
                            Stroke(at,(at+dir).normalized,.0010f,sprinkle%2==0?new Color(1f,.89f,.56f):new Color(.71f,.89f,.97f));}
                        break;
                }
            }
            mesh=new Mesh{name="Planet silhouette batch / "+rules.Kind};
            mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetUVs(0,motion);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;material=new Material(Resources.Load<Shader>("PlanetBiomeDecor"));
            material.SetFloat("_Sway",rules.Kind==BattlePlanet.Tropical||rules.Kind==BattlePlanet.Oceanic?.075f:rules.Kind==BattlePlanet.Amber?.025f:0);
            material.SetFloat("_Pulse",rules.Kind==BattlePlanet.Volcanic||rules.Kind==BattlePlanet.Storm?1f:.35f);
            var renderer=gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            Debug.Log("BIOME_DECOR_READY kind="+rules.Kind+" zones="+count+" vertices="+vertices.Count+" colliders=0 uniqueSilhouettes=true");
        }
        private void OnDestroy()
        {
            if(mesh!=null){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}
            if(material!=null){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}
        }
    }
}
