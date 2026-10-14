using System;
using System.IO;
using UnityEngine;
namespace BrainrotPlanet
{
    public enum BattlePlanet : byte { Tropical, Desert, Moon }
    public sealed class PlanetBiomeRules
    {
        public readonly BattlePlanet Kind;
        public readonly int Seed;
        public readonly Vector3[] Zones;
        public const float ZoneRadius=.135f;
        public const float GrassDuration=.9f;
        public float Gravity=>Kind==BattlePlanet.Moon?17f:23f;
        public float JumpImpulse=>Kind==BattlePlanet.Moon?7.4f:6.2f;
        public string Title=>Kind==BattlePlanet.Tropical?"ТРОПИЧЕСКАЯ ПЛАНЕТА":Kind==BattlePlanet.Desert?"ПЕСЧАНАЯ ПЛАНЕТА":"ЛУННАЯ ПЛАНЕТА";
        public string Hint=>Kind==BattlePlanet.Tropical?"Трава ненадолго замедляет. Перепрыгивай островки!":Kind==BattlePlanet.Desert?"Попутный ветер немного ускоряет. Следуй светлым стрелкам!":"Низкая гравитация: прыжки выше, полёт дольше.";
        public Color Sky=>Kind==BattlePlanet.Tropical?new Color(.045f,.16f,.22f):Kind==BattlePlanet.Desert?new Color(.27f,.12f,.14f):new Color(.055f,.06f,.18f);
        public Color Accent=>Kind==BattlePlanet.Tropical?new Color(.28f,.9f,.65f):Kind==BattlePlanet.Desert?new Color(1f,.77f,.34f):new Color(.64f,.7f,1f);
        public static int Next(int previous)=>previous<0?0:(previous+1)%3;
        public static void Validate(BattlePlanet kind,int seed)
        {if((int)kind>2||seed<=0)throw new InvalidDataException("Invalid battle planet");}
        public PlanetBiomeRules(BattlePlanet kind,int seed)
        {
            Validate(kind,seed);Kind=kind;Seed=seed;
            if(kind==BattlePlanet.Moon){Zones=Array.Empty<Vector3>();return;}
            var candidates=new System.Collections.Generic.List<Vector3>();
            uint random=(uint)seed;
            Vector3 opening=new Vector3(.91f,.20f,.33f).normalized;
            Basis(opening,out Vector3 east,out Vector3 north);
            for(int i=0;i<4;i++)
            {
                float angle=(45+i*90)*Mathf.Deg2Rad;
                candidates.Add((opening*Mathf.Cos(.46f)+(east*Mathf.Cos(angle)+north*Mathf.Sin(angle))*Mathf.Sin(.46f)).normalized);
            }
            for(int i=0;i<160&&candidates.Count<18;i++)
            {
                float y=Unit(ref random)*2-1,angle=Unit(ref random)*Mathf.PI*2;
                float radius=Mathf.Sqrt(1-y*y);
                Vector3 direction=new Vector3(radius*Mathf.Cos(angle),y,radius*Mathf.Sin(angle));
                if(!SafeFromSpawns(direction))continue;
                bool overlap=false;foreach(var existing in candidates)if(Vector3.Dot(existing,direction)>Mathf.Cos(ZoneRadius*2.7f)){overlap=true;break;}
                if(!overlap)candidates.Add(direction);
            }
            Zones=candidates.ToArray();
        }
        public static float Unit(ref uint state)
        {state^=state<<13;state^=state>>17;state^=state<<5;return (state&65535)/65535f;}
        public static void Basis(Vector3 normal,out Vector3 east,out Vector3 north)
        {
            east=Vector3.Cross(Vector3.up,normal);
            if(east.sqrMagnitude<.01f)east=Vector3.Cross(Vector3.right,normal);
            east.Normalize();north=Vector3.Cross(normal,east).normalized;
        }
        public static bool SafeFromSpawns(Vector3 direction)
        {
            Vector3 centre=new Vector3(.91f,.20f,.33f).normalized;
            Vector3 east=Vector3.Cross(Vector3.up,centre).normalized,north=Vector3.Cross(centre,east).normalized;
            float margin=Mathf.Cos(ZoneRadius+.235f);
            if(Vector3.Dot(direction,centre)>margin)return false;
            foreach(var difficulty in new[]{AiDifficulty.Easy,AiDifficulty.Medium,AiDifficulty.Hard})
            {
                float radial=Mathf.Tan(AiBalanceTuning.SpawnAngle(difficulty)*Mathf.Deg2Rad);
                foreach(var axis in new[]{east,-east,north})
                    if(Vector3.Dot(direction,(centre+axis*radial).normalized)>margin)return false;
            }
            return true;
        }
        public int ZoneAt(Vector3 direction,out float weight)
        {
            weight=0;Vector3 n=direction.normalized;float edge=Mathf.Cos(ZoneRadius);
            for(int i=0;i<Zones.Length;i++)
            {
                float dot=Vector3.Dot(n,Zones[i]);if(dot<=edge)continue;
                weight=Mathf.SmoothStep(0,1,Mathf.Clamp01((dot-edge)/(1-edge)*3));return i;
            }
            return -1;
        }
        public Vector3 WindDirection(int zone)
        {Basis(Zones[zone],out Vector3 east,out _);return east;}
        public Color Ground(Vector3 direction,float altitude,float noise)
        {
            if(Kind==BattlePlanet.Moon)
                return Color.Lerp(new Color(.49f,.54f,.67f),new Color(.79f,.83f,.89f),noise);
            if(Kind==BattlePlanet.Desert)
                return Color.Lerp(new Color(.78f,.48f,.24f),new Color(1f,.85f,.52f),noise);
            Color ground=Color.Lerp(new Color(.26f,.66f,.52f),new Color(.62f,.87f,.72f),noise);
            if(ZoneAt(direction,out float weight)>=0)ground=Color.Lerp(ground,new Color(.12f,.49f,.28f),weight*.55f);
            if(altitude>1.95f)ground=Color.Lerp(ground,new Color(.71f,.79f,.72f),.55f);
            return ground;
        }
    }
    public sealed class PlanetZoneMotion
    {
        private int previousZone=-1;
        private float grassTime;
        public void Reset(){previousZone=-1;grassTime=0;}
        public float Tick(PlanetBiomeRules rules,Vector3 normal,Vector3 wish,bool grounded,float dt)
        {
            if(rules==null||!grounded){Reset();return 1;}
            int zone=rules.ZoneAt(normal,out float weight);
            if(zone<0){Reset();return 1;}
            if(rules.Kind==BattlePlanet.Desert)
            {
                float alignment=Vector3.Dot(wish.normalized,rules.WindDirection(zone));
                return 1+.16f*weight*Mathf.Clamp01((alignment-.15f)/.85f);
            }
            if(rules.Kind!=BattlePlanet.Tropical)return 1;
            if(zone!=previousZone){previousZone=zone;grassTime=0;}
            grassTime+=Mathf.Max(0,dt);
            return grassTime<PlanetBiomeRules.GrassDuration?1-.20f*weight:1;
        }
    }
}
