using System.Collections.Generic;
using UnityEngine;
namespace BrainrotPlanet
{
    // Batched curved foliage / wind ribbons. No colliders, primitives or blockers.
    public sealed class PlanetBiomeDecor : MonoBehaviour
    {
        private Mesh mesh;private Material material;
        public void Build(PlanetBiomeRules rules)
        {
            var v=new List<Vector3>();var c=new List<Color>();var t=new List<int>();
            uint random=(uint)rules.Seed;
            for(int zone=0;zone<rules.Zones.Length;zone++)
            {
                Vector3 n=rules.Zones[zone];
                PlanetBiomeRules.Basis(n,out Vector3 east,out Vector3 north);
                if(rules.HasFoliage)
                {
                    // One batched mesh per map, with no physics or separate props.
                    for(int blade=0;blade<(rules.Kind==BattlePlanet.Tropical?72:48);blade++)
                    {
                        float angle=PlanetBiomeRules.Unit(ref random)*Mathf.PI*2;
                        float distance=Mathf.Sqrt(PlanetBiomeRules.Unit(ref random))*PlanetBiomeRules.ZoneRadius*.88f;
                        Vector3 direction=(n+(east*Mathf.Cos(angle)+north*Mathf.Sin(angle))*distance).normalized;
                        Vector3 root=PlanetSurface.Point(direction,.035f);
                        float height=.22f+PlanetBiomeRules.Unit(ref random)*.32f;
                        float turn=PlanetBiomeRules.Unit(ref random)*Mathf.PI*2;
                        Vector3 side=(east*Mathf.Cos(turn)+north*Mathf.Sin(turn)).normalized;
                        Vector3 bend=Vector3.Cross(direction,side)*height*.34f;
                        Color shade=rules.Kind==BattlePlanet.Oceanic?
                            Color.Lerp(new Color(.07f,.46f,.52f),new Color(.42f,.88f,.79f),PlanetBiomeRules.Unit(ref random)):
                            rules.Kind==BattlePlanet.Candy?
                            Color.Lerp(new Color(.75f,.30f,.65f),new Color(1f,.70f,.82f),PlanetBiomeRules.Unit(ref random)):
                            Color.Lerp(new Color(.08f,.42f,.20f),new Color(.56f,.88f,.28f),PlanetBiomeRules.Unit(ref random));
                        for(int segment=0;segment<3;segment++)
                        {
                            float a=segment/3f,b=(segment+1)/3f;
                            Vector3 from=root+direction*(height*a)+bend*a*a,to=root+direction*(height*b)+bend*b*b;
                            float wa=(1-a)*.045f,wb=(1-b)*.045f;
                            Quad(v,c,t,from-side*wa,from+side*wa,to-side*wb,to+side*wb,Color.Lerp(shade,rules.Accent,b*.25f));
                        }
                    }
                    // Small coloured flowers add contrast without hiding the paint.
                    for(int flower=0;flower<6;flower++)
                    {
                        float angle=flower*Mathf.PI/3+.35f;
                        Vector3 direction=(n+(east*Mathf.Cos(angle)+north*Mathf.Sin(angle))*.075f).normalized;
                        Vector3 root=PlanetSurface.Point(direction,.17f);
                        for(int petal=0;petal<5;petal++)
                        {
                            float a=petal*Mathf.PI*2/5,b=(petal+1)*Mathf.PI*2/5;
                            Vector3 p=root+(east*Mathf.Cos(a)+north*Mathf.Sin(a))*.12f;
                            Vector3 q=root+(east*Mathf.Cos(b)+north*Mathf.Sin(b))*.12f;
                            int flowerVertex=v.Count;v.Add(root);v.Add(p);v.Add(q);
                            Color petalColor=flower%2==0?rules.Accent:Color.Lerp(rules.Accent,Color.white,.55f);
                            c.Add(petalColor);c.Add(petalColor);c.Add(petalColor);
                            t.Add(flowerVertex);t.Add(flowerVertex+1);t.Add(flowerVertex+2);
                        }
                    }
                }
                else if(rules.HasWind)
                {
                    Vector3 wind=rules.WindDirection(zone),across=Vector3.Cross(n,wind);
                    for(int arrow=0;arrow<3;arrow++)
                    {
                        Vector3 centre=(n+wind*(arrow-1)*.065f).normalized;
                        for(int side=-1;side<=1;side+=2)
                        {
                            Vector3 a=(centre+across*side*.029f-wind*.017f).normalized,b=(centre+wind*.025f).normalized;
                            Vector3 width=across*.0028f;
                            Quad(v,c,t,PlanetSurface.Point((a-width).normalized,.025f),PlanetSurface.Point((a+width).normalized,.025f),
                                PlanetSurface.Point((b-width).normalized,.025f),PlanetSurface.Point((b+width).normalized,.025f),rules.Accent);
                        }
                    }
                }
                else
                {
                    // Low, translucent-looking facets distinguish ice, lava,
                    // crystals and spring fields without creating obstacles.
                    for(int facet=0;facet<12;facet++)
                    {
                        float angle=facet*Mathf.PI/6f;
                        Vector3 axis=east*Mathf.Cos(angle)+north*Mathf.Sin(angle);
                        Vector3 baseDirection=(n+axis*.045f).normalized;
                        Vector3 basePoint=PlanetSurface.Point(baseDirection,.035f);
                        float height=rules.Kind==BattlePlanet.Crystal?.42f:.16f;
                        Vector3 tip=basePoint+baseDirection*height;
                        Vector3 width=Vector3.Cross(baseDirection,axis).normalized*.09f;
                        Quad(v,c,t,basePoint-width,basePoint+width,tip-width*.16f,tip+width*.16f,
                            Color.Lerp(rules.Accent,Color.white,facet%3*.16f));
                    }
                }
            }
            mesh=new Mesh{name="Batched "+rules.Kind+" biome details"};
            mesh.SetVertices(v);mesh.SetColors(c);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            material=new Material(Resources.Load<Shader>("PlanetBiomeDecor"));
            var renderer=gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            Debug.Log("BIOME_DECOR_READY kind="+rules.Kind+" zones="+rules.Zones.Length+" vertices="+v.Count+" colliders=0");
        }
        private static void Quad(List<Vector3> v,List<Color> c,List<int> t,Vector3 a,Vector3 b,Vector3 d,Vector3 e,Color color)
        {
            int i=v.Count;v.Add(a);v.Add(b);v.Add(d);v.Add(e);
            for(int k=0;k<4;k++)c.Add(color);
            t.Add(i);t.Add(i+2);t.Add(i+1);t.Add(i+1);t.Add(i+2);t.Add(i+3);
        }
        private void OnDestroy()
        {
            if(mesh!=null){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}
            if(material!=null){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}
        }
    }
}
