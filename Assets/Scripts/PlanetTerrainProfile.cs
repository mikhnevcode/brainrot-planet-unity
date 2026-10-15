using System.Collections.Generic;
using UnityEngine;
namespace BrainrotPlanet
{
    // Immutable, seeded geometry shared by the client and its authoritative room.
    // Every feature is an isolated cap; decoration never supplies collision.
    public sealed class PlanetTerrainProfile
    {
        public readonly struct Landform
        {
            public readonly Vector3 Direction;
            public readonly float Radius,Height,Rim,InverseEdge;
            public Landform(Vector3 direction,float radius,float height,float rim=0)
            {Direction=direction;Radius=radius;Height=height;Rim=rim;InverseEdge=1/(1-Mathf.Cos(radius));}
            public float Radial(Vector3 n)=>(1-Vector3.Dot(n,Direction))*InverseEdge;
            public float Cap(Vector3 n)
            {float t=Mathf.Clamp01(1-Radial(n));return t*t*(3-2*t);}
        }
        public readonly BattlePlanet Kind;
        public readonly Landform[] Mounds,Bowls;
        private readonly float offset;
        public PlanetTerrainProfile(BattlePlanet kind,int seed)
        {
            Kind=kind;offset=(seed%997)*.019f+(int)kind*11.7f;
            uint random=unchecked((uint)seed^((uint)kind+1)*0x9e3779b9u)|1u;
            var mounds=new List<Landform>();var bowls=new List<Landform>();
            int hills=kind==BattlePlanet.Moon?5:kind==BattlePlanet.Crystal?8:7;
            int craters=kind==BattlePlanet.Moon?13:kind==BattlePlanet.Oceanic?9:7;
            Vector3 opening=new Vector3(.91f,.20f,.33f).normalized;
            PlanetBiomeRules.Basis(opening,out Vector3 east,out Vector3 north);
            Vector3 showcase=(opening*Mathf.Cos(.46f)+(east+north).normalized*Mathf.Sin(.46f)).normalized;
            for(int type=0;type<2;type++)
            {
                int wanted=type==0?hills:craters;
                var list=type==0?mounds:bowls;
                for(int attempt=0;attempt<2200&&list.Count<wanted;attempt++)
                {
                    float y=PlanetBiomeRules.Unit(ref random)*2-1,a=PlanetBiomeRules.Unit(ref random)*Mathf.PI*2;
                    float r=Mathf.Sqrt(1-y*y);
                    Vector3 n=type==0&&attempt==0?showcase:new Vector3(r*Mathf.Cos(a),y,r*Mathf.Sin(a));
                    float size=.17f+PlanetBiomeRules.Unit(ref random)*.055f;
                    if(type==0)size+=kind==BattlePlanet.Volcanic?.045f:kind==BattlePlanet.Oceanic?.02f:0;
                    if(!PlanetBiomeRules.SafeFromSpawns(n)||Overlaps(n,size,mounds)||Overlaps(n,size,bowls))continue;
                    float height=type==0?1.1f+PlanetBiomeRules.Unit(ref random)*.85f:.40f+PlanetBiomeRules.Unit(ref random)*.23f;
                    if(type==0&&kind==BattlePlanet.Oceanic)height*=.72f;
                    if(type==0&&kind==BattlePlanet.Moon)height*=.65f;
                    if(type==0&&kind==BattlePlanet.Volcanic)height*=1.18f;
                    height=Mathf.Min(height,size*PlanetMath.Radius*(type==0?.34f:.18f));
                    float rim=type==1?(kind==BattlePlanet.Moon?.28f:.16f)+PlanetBiomeRules.Unit(ref random)*.10f:0;
                    list.Add(new Landform(n,size,height,rim));
                }
            }
            Mounds=mounds.ToArray();Bowls=bowls.ToArray();
        }
        private static bool Overlaps(Vector3 n,float size,List<Landform> forms)
        {
            foreach(var form in forms)
                if(Vector3.Dot(n,form.Direction)>Mathf.Cos(size+form.Radius+.025f))return true;
            return false;
        }
        public bool IsHill(Vector3 n)
        {foreach(var hill in Mounds)if(hill.Radial(n)<1.12f)return true;return false;}
        public float Elevation(Vector3 n)
        {
            float coarse=Mathf.PerlinNoise(n.x*5.2f+14.8f+offset,n.z*5.2f+n.y*1.7f+3.1f);
            float fine=Mathf.PerlinNoise(n.y*13f+9.2f,n.x*11f+n.z*4.3f+offset);
            float height=.72f+.22f*coarse+.045f*fine;
            if(Kind==BattlePlanet.Frost)height+=.08f*Mathf.Abs(n.y);
            if(Kind==BattlePlanet.Desert)height+=.04f*Mathf.Sin(n.x*33+n.z*13)*fine;
            foreach(var hill in Mounds)
            {
                float cap=hill.Cap(n);if(cap<=0)continue;
                float shape=Kind==BattlePlanet.Frost||Kind==BattlePlanet.Candy?cap:cap*cap;
                if(Kind==BattlePlanet.Crystal)shape=cap*(.64f+.36f*cap);
                if(Kind==BattlePlanet.Volcanic)
                {
                    // A walkable cone with a sunken, rounded caldera, not a cylinder.
                    float crater=Mathf.Clamp01((cap-.74f)/.26f);
                    shape=cap*.86f-.46f*crater*crater*(3-2*crater);
                }
                height+=hill.Height*shape;
            }
            foreach(var bowl in Bowls)
            {
                float radial=bowl.Radial(n);
                float floor=Mathf.Clamp01(1-radial);floor=floor*floor*(3-2*floor);
                float rim=Mathf.Clamp01(1-Mathf.Abs(radial-1)/.50f);rim=rim*rim*(3-2*rim);
                height-=bowl.Height*floor;height+=bowl.Rim*rim;
            }
            return Mathf.Max(.08f,height);
        }
        public float ReliefTone(Vector3 n,float noise)
        {
            float tone=.98f+(noise-.5f)*.09f;
            foreach(var hill in Mounds)tone+=.10f*hill.Cap(n);
            foreach(var bowl in Bowls)
            {
                float radial=bowl.Radial(n);
                tone-=.15f*Mathf.Clamp01(1-radial);
                tone+=.09f*Mathf.Clamp01(1-Mathf.Abs(radial-1)/.34f);
            }
            return Mathf.Clamp(tone,.80f,1.13f);
        }
        public Color ColorDetail(Vector3 n,float altitude,Color ground)
        {
            float mound=0,bowl=0,rim=0;
            foreach(var hill in Mounds)mound=Mathf.Max(mound,hill.Cap(n));
            foreach(var form in Bowls)
            {
                float r=form.Radial(n);bowl=Mathf.Max(bowl,Mathf.Clamp01(1-r));
                rim=Mathf.Max(rim,Mathf.Clamp01(1-Mathf.Abs(r-1)/.3f));
            }
            switch(Kind)
            {
                case BattlePlanet.Tropical:return Color.Lerp(ground,new Color(.79f,.78f,.51f),bowl*.55f);
                case BattlePlanet.Desert:return Color.Lerp(ground,new Color(.65f,.39f,.22f),rim*.20f);
                case BattlePlanet.Moon:return Color.Lerp(ground,new Color(.30f,.35f,.46f),bowl*.36f);
                case BattlePlanet.Frost:return Color.Lerp(ground,new Color(.92f,.98f,1f),mound*.75f);
                case BattlePlanet.Volcanic:
                    return Color.Lerp(Color.Lerp(ground,new Color(1f,.46f,.12f),bowl*.9f),new Color(.82f,.58f,.36f),mound*.22f);
                case BattlePlanet.Oceanic:
                    return Color.Lerp(Color.Lerp(ground,new Color(.09f,.50f,.65f),bowl*.7f),new Color(.90f,.86f,.65f),mound*.85f);
                case BattlePlanet.Crystal:return Color.Lerp(ground,new Color(.83f,.82f,.98f),mound*.4f);
                case BattlePlanet.Storm:return Color.Lerp(ground,new Color(.33f,.43f,.56f),mound*.42f);
                case BattlePlanet.Amber:return Color.Lerp(ground,new Color(1f,.85f,.48f),mound*.5f+rim*.12f);
                case BattlePlanet.Candy:
                    return Color.Lerp(Color.Lerp(ground,new Color(.45f,.25f,.30f),bowl*.55f),new Color(1f,.92f,.78f),mound*.8f);
                default:return ground;
            }
        }
    }
}
