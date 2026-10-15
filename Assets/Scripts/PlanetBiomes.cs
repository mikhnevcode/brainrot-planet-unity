using System;
using System.IO;
using UnityEngine;
namespace BrainrotPlanet
{
    // IDs are serialized in match packets: keep the original three stable.
    public enum BattlePlanet : byte
    { Tropical, Desert, Moon, Frost, Volcanic, Oceanic, Crystal, Storm, Amber, Candy }
    public sealed class PlanetBiomeRules
    {
        public readonly BattlePlanet Kind;
        public readonly int Seed;
        public readonly Vector3[] Zones;
        public readonly PlanetTerrainProfile Terrain;
        public const float ZoneRadius=.135f;
        public const float GrassDuration=.9f;
        public const int Count=10;
        private static readonly string[] Titles={
            "ТРОПИЧЕСКАЯ ПЛАНЕТА","ПЕСЧАНАЯ ПЛАНЕТА","ЛУННАЯ ПЛАНЕТА",
            "ЛЕДЯНАЯ ПЛАНЕТА","ВУЛКАНИЧЕСКАЯ ПЛАНЕТА","ОКЕАНСКАЯ ПЛАНЕТА",
            "КРИСТАЛЬНАЯ ПЛАНЕТА","ГРОЗОВАЯ ПЛАНЕТА","ЯНТАРНАЯ ПЛАНЕТА",
            "КАРАМЕЛЬНАЯ ПЛАНЕТА"};
        private static readonly string[] Hints={
            "Пальмы и луговые холмы. Трава ненадолго замедляет.",
            "Песчаные холмы. Попутный ветер ускоряет, встречный слегка замедляет.",
            "Ударные кратеры. Прыжки выше, полёт дольше.",
            "Снежные купола. Ледяные пятна слегка ускоряют.",
            "Пологие кальдеры. Тёплые потоки ускоряют и усиливают прыжки.",
            "Песчаные островки. Мелководье замедляет ненадолго.",
            "Кристальные склоны. Разгоняйся и прыгай выше.",
            "Грозовые прожилки. По ветру быстрее, против ветра медленнее.",
            "Янтарные холмы и золотистые камни. Следуй ветру для ускорения.",
            "Глазурные купола. Пружинные поля ускоряют и усиливают прыжки."};
        private static readonly Color[] Skies={
            new Color(.045f,.16f,.22f),new Color(.27f,.12f,.14f),new Color(.055f,.06f,.18f),
            new Color(.07f,.18f,.28f),new Color(.24f,.075f,.09f),new Color(.035f,.14f,.24f),
            new Color(.16f,.075f,.26f),new Color(.095f,.075f,.21f),new Color(.23f,.12f,.10f),
            new Color(.24f,.12f,.25f)};
        private static readonly Color[] Accents={
            new Color(.28f,.9f,.65f),new Color(1f,.77f,.34f),new Color(.64f,.7f,1f),
            new Color(.62f,.95f,1f),new Color(1f,.42f,.19f),new Color(.24f,.83f,1f),
            new Color(.85f,.52f,1f),new Color(.55f,.72f,1f),new Color(1f,.68f,.25f),
            new Color(1f,.63f,.80f)};
        public float Gravity=>Kind==BattlePlanet.Moon?17f:Kind==BattlePlanet.Storm?20f:23f;
        public float JumpImpulse=>Kind==BattlePlanet.Moon?7.4f:
            Kind==BattlePlanet.Candy?7f:
            Kind==BattlePlanet.Crystal?6.9f:
            Kind==BattlePlanet.Volcanic?6.7f:6.2f;
        public string Title=>Titles[(int)Kind];
        public string Hint=>Hints[(int)Kind];
        public Color Sky=>Skies[(int)Kind];
        public Color Accent=>Accents[(int)Kind];
        public static string TitleOf(BattlePlanet kind)
        {
            int index=(int)kind;
            if(index<0||index>=Count)throw new ArgumentOutOfRangeException(nameof(kind));
            return Titles[index];
        }
        public static Color OrbitAccent(int planet)=>Accents[planet];
        public bool HasFoliage=>Kind==BattlePlanet.Tropical||Kind==BattlePlanet.Oceanic||Kind==BattlePlanet.Candy;
        public bool HasWind=>Kind==BattlePlanet.Desert||Kind==BattlePlanet.Storm||Kind==BattlePlanet.Amber;
        public static int Next(int previous)=>previous<0?0:(previous+1)%Count;
        public static void Validate(BattlePlanet kind,int seed)
        {if((int)kind>=Count||seed<=0)throw new InvalidDataException("Invalid battle planet");}
        public PlanetBiomeRules(BattlePlanet kind,int seed)
        {
            Validate(kind,seed);Kind=kind;Seed=seed;Terrain=new PlanetTerrainProfile(kind,seed);
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
            Color dark,light,zone;
            switch(Kind)
            {
                case BattlePlanet.Desert: dark=new Color(.78f,.48f,.24f);light=new Color(1f,.85f,.52f);zone=new Color(1f,.92f,.69f);break;
                case BattlePlanet.Moon: dark=new Color(.49f,.54f,.67f);light=new Color(.79f,.83f,.89f);zone=light;break;
                case BattlePlanet.Frost: dark=new Color(.25f,.58f,.73f);light=new Color(.83f,.96f,1f);zone=new Color(.42f,.87f,.95f);break;
                case BattlePlanet.Volcanic: dark=new Color(.22f,.20f,.25f);light=new Color(.53f,.32f,.28f);zone=new Color(1f,.43f,.16f);break;
                case BattlePlanet.Oceanic: dark=new Color(.10f,.49f,.68f);light=new Color(.42f,.80f,.77f);zone=new Color(.11f,.65f,.67f);break;
                case BattlePlanet.Crystal: dark=new Color(.36f,.27f,.67f);light=new Color(.80f,.62f,.96f);zone=new Color(.86f,.77f,1f);break;
                case BattlePlanet.Storm: dark=new Color(.28f,.31f,.52f);light=new Color(.60f,.65f,.83f);zone=new Color(.67f,.83f,1f);break;
                case BattlePlanet.Amber: dark=new Color(.60f,.35f,.17f);light=new Color(1f,.78f,.39f);zone=new Color(1f,.88f,.53f);break;
                case BattlePlanet.Candy: dark=new Color(.70f,.39f,.69f);light=new Color(1f,.77f,.79f);zone=new Color(.96f,.54f,.78f);break;
                default: dark=new Color(.26f,.66f,.52f);light=new Color(.62f,.87f,.72f);zone=new Color(.12f,.49f,.28f);break;
            }
            Color ground=Color.Lerp(dark,light,noise);
            if(ZoneAt(direction,out float weight)>=0)ground=Color.Lerp(ground,zone,weight*.50f);
            if(altitude>1.95f)ground=Color.Lerp(ground,light,.48f);
            return Terrain.ColorDetail(direction,altitude,ground);
        }
    }
    public enum BiomeMotionEffect { None, Tailwind, Headwind, GrassSlow, SurfaceBoost }
    public sealed class PlanetZoneMotion
    {
        private int previousZone=-1;
        private float grassTime;
        public BiomeMotionEffect Effect {get;private set;}
        public int CurrentZone {get;private set;}=-1;
        public float Multiplier {get;private set;}=1f;
        private float Result(BiomeMotionEffect effect,int zone,float multiplier)
        {Effect=effect;CurrentZone=zone;Multiplier=multiplier;return multiplier;}
        public void Reset(){previousZone=-1;grassTime=0;Effect=BiomeMotionEffect.None;CurrentZone=-1;Multiplier=1f;}
        public float Tick(PlanetBiomeRules rules,Vector3 normal,Vector3 wish,bool grounded,float dt)
        {
            if(rules==null||!grounded){Reset();return 1;}
            int zone=rules.ZoneAt(normal,out float weight);
            if(zone<0){Reset();return 1;}
            if(rules.HasWind)
            {
                float alignment=Vector3.Dot(wish.normalized,rules.WindDirection(zone));
                float boost=rules.Kind==BattlePlanet.Storm?.20f:rules.Kind==BattlePlanet.Amber?.13f:.16f;
                if(alignment>.15f)
                    return Result(BiomeMotionEffect.Tailwind,zone,
                        1+boost*weight*Mathf.Clamp01((alignment-.15f)/.85f));
                if(alignment<-.15f)
                {
                    float drag=rules.Kind==BattlePlanet.Storm?.10f:rules.Kind==BattlePlanet.Amber?.07f:.08f;
                    return Result(BiomeMotionEffect.Headwind,zone,
                        1-drag*weight*Mathf.Clamp01((-alignment-.15f)/.85f));
                }
                return Result(BiomeMotionEffect.None,zone,1);
            }
            if(rules.Kind==BattlePlanet.Frost||rules.Kind==BattlePlanet.Volcanic||
               rules.Kind==BattlePlanet.Crystal||rules.Kind==BattlePlanet.Candy)
                return Result(BiomeMotionEffect.SurfaceBoost,zone,
                    1+(rules.Kind==BattlePlanet.Frost?.12f:.09f)*weight);
            if(rules.Kind!=BattlePlanet.Tropical&&rules.Kind!=BattlePlanet.Oceanic)
                return Result(BiomeMotionEffect.None,zone,1);
            if(zone!=previousZone){previousZone=zone;grassTime=0;}
            grassTime+=Mathf.Max(0,dt);
            float duration=rules.Kind==BattlePlanet.Oceanic?.65f:PlanetBiomeRules.GrassDuration;
            return grassTime<duration
                ?Result(BiomeMotionEffect.GrassSlow,zone,
                    1-(rules.Kind==BattlePlanet.Oceanic?.14f:.20f)*weight)
                :Result(BiomeMotionEffect.None,zone,1);
        }
    }
}
