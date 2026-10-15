using System.Collections.Generic;
using UnityEngine;

namespace BrainrotPlanet
{
    public sealed class PlanetEffects : MonoBehaviour
    {
        private readonly ParticleSystem[] pool=new ParticleSystem[14];
        private int next;
        private Material softMaterial;
        private void Awake()
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
            softMaterial=new Material(Resources.Load<Shader>("SoftSpark"));
            for(int i=0;i<pool.Length;i++)
            {
                var go=new GameObject("Pooled paint sparkle "+i);go.transform.SetParent(transform,false);
                var particle=go.AddComponent<ParticleSystem>();particle.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=particle.main;main.loop=false;main.playOnAwake=false;main.duration=1.3f;
                main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=96;
                main.startLifetime=new ParticleSystem.MinMaxCurve(.3f,.85f);main.gravityModifier=0;
                var emission=particle.emission;emission.rateOverTime=0;
                var shape=particle.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=65;shape.radius=.25f;
                var renderer=particle.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=softMaterial;
                var size=particle.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,1,1,0));
                var color=particle.colorOverLifetime;color.enabled=true;var gradient=new Gradient();
                gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});color.color=gradient;
                pool[i]=particle;
            }
        }
        public void Burst(Vector3 position,Color color,int amount,float speed)
        {
            if(softMaterial==null)return;
            var particle=pool[next];next=(next+1)%pool.Length;
            particle.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.transform.position=position;
            particle.transform.rotation=Quaternion.FromToRotation(Vector3.forward,position.normalized);
            var main=particle.main;
            main.startSpeed=new ParticleSystem.MinMaxCurve(speed*.35f,speed*.85f);
            main.startSize=new ParticleSystem.MinMaxCurve(.08f,.29f);main.startColor=color;
            particle.Play();particle.Emit(Mathf.Min(amount,96));
        }
        private void OnDestroy(){if(softMaterial!=null)Destroy(softMaterial);}
        /// <summary>
        /// One small themed wave and burst per skill activation (not per frame).
        /// Reuses existing particle materials; unique color and radius per hero.
        /// </summary>
        public void HeroAbility(int hero,Vector3 direction,Color teamColor)
        {
            if(softMaterial==null)return;
            OriginalSignatureFx.Play(this,hero,direction,teamColor);
        }
        private void LegacyHeroAbility(int hero,Vector3 direction,Color teamColor)
        {
            if(softMaterial==null)return;
            Color accent;
            switch(hero)
            {
                case 0:accent=new Color(.19f,.76f,1f);break;    // Shark water
                case 1:accent=new Color(1f,.58f,.11f);break;   // Bombardiro blast
                case 2:accent=new Color(.37f,.91f,1f);break;   // Ice
                case 3:accent=new Color(.94f,.65f,.21f);break; // Turbine exhaust
                case 4:accent=new Color(.94f,.55f,.66f);break; // Coffee
                case 5:accent=new Color(.60f,.36f,.84f);break; // Ninja smoke
                case 6:accent=new Color(.34f,.85f,.33f);break; // Forest
                case 7:accent=new Color(1f,.67f,.26f);break;   // Drum pulse
                case 8:accent=new Color(.55f,.94f,.26f);break; // Cactus
                case 9:accent=new Color(.40f,.88f,1f);break;   // Bird swirl
                case 10:accent=new Color(.44f,.99f,.33f);break;// Spring
                case 12:accent=new Color(.16f,.85f,.61f);break;// Fish-bear splash
                default:accent=new Color(1f,.50f,.44f);break; // Shrimp
            }
            Vector3 n=direction.normalized;
            Burst(PlanetSurface.Point(n,1.1f),Color.Lerp(accent,teamColor,.35f),
                  (hero==1||hero==7?36:20),2.5f+(hero%3));
            var ring=new GameObject("Signature hero power "+hero);
            var effect=ring.AddComponent<PaintWave>();
            effect.Configure(n,hero==7 ? 5.5f : hero==10 ? 3.9f : 3.0f,
                             hero==4 ? .95f : .67f,accent);
            Debug.Log("HERO_FX_ACTIVATED hero="+hero);
        }

        public void CaptureFlash(Vector3 direction,Color color,int count)
        {
            if(softMaterial==null)return;
            Vector3 at=PlanetSurface.Point(direction,.7f);
            Burst(at,color,Mathf.Clamp(12+count/60,18,72),3.2f);
            var go=new GameObject("Circular paint wave on globe");
            var wave=go.AddComponent<PaintWave>();
            wave.Configure(direction,Mathf.Clamp(1.2f+count/260f,2f,7f),.65f,color);
        }

        public struct CrystalFxProfile
        {
            public int Waves,FireworkBursts;
            public float OuterRadius;
            public CrystalFxProfile(int waves,int fireworks,float radius)
            {Waves=waves;FireworkBursts=fireworks;OuterRadius=radius;}
        }
        public static CrystalFxProfile CrystalProfile(bool rare)=>rare
            ? new CrystalFxProfile(2,5,6.4f)
            : new CrystalFxProfile(2,0,4.5f);

        /// <summary>A cyan surface ripple for a normal pickup; a larger gold
        /// ripple followed by a short multicolour firework for a rare pickup.</summary>
        public void CrystalPickup(Vector3 direction,bool rare,Color teamColor)
        {
            if(softMaterial==null)return;
            Vector3 n=direction.normalized;
            CrystalFxProfile profile=CrystalProfile(rare);
            Color primary=rare?new Color(1f,.72f,.08f):new Color(.18f,1f,1f);
            Color secondary=rare?new Color(1f,.33f,.66f):Color.Lerp(primary,teamColor,.42f);
            Burst(PlanetSurface.Point(n,1.08f),primary,rare?42:28,rare?4.8f:3.2f);
            MakeCrystalWave(rare?"Rare crystal gold paint wave":"Common crystal cyan paint wave",
                n,profile.OuterRadius,rare?.92f:.78f,primary);
            MakeCrystalWave(rare?"Rare crystal colour paint wave":"Common crystal team paint wave",
                n,profile.OuterRadius*.68f,rare?.64f:.52f,secondary);
            if(rare)
            {
                var celebration=new GameObject("Rare crystal multicolour fireworks");
                celebration.transform.SetParent(transform,false);
                celebration.AddComponent<CrystalFireworks>().Configure(this,n,profile.FireworkBursts);
            }
            Debug.Log("CRYSTAL_FX_PLAYED rare="+rare+" waves="+profile.Waves+
                " fireworks="+profile.FireworkBursts);
        }
        private void MakeCrystalWave(string title,Vector3 direction,float radius,float duration,Color color)
        {
            var go=new GameObject(title);go.transform.SetParent(transform,false);
            go.AddComponent<PaintWave>().Configure(direction,radius,duration,color);
        }
    }

    /// <summary>Five staggered fountains make the rare crystal recognisable
    /// without allocating a second particle system implementation.</summary>
    public sealed class CrystalFireworks : MonoBehaviour
    {
        private static readonly Color[] Palette={
            new Color(1f,.76f,.08f),new Color(1f,.24f,.62f),
            new Color(.20f,.92f,1f),new Color(.50f,1f,.20f),
            new Color(1f,.42f,.10f)};
        private PlanetEffects effects;
        private Vector3 center,tangentX,tangentY;
        private int target,index;
        private float age,nextBurst;
        public void Configure(PlanetEffects source,Vector3 direction,int burstCount)
        {
            effects=source;center=direction.normalized;target=Mathf.Max(1,burstCount);
            tangentX=Vector3.Cross(center,Vector3.up).normalized;
            if(tangentX.sqrMagnitude<.1f)tangentX=Vector3.Cross(center,Vector3.forward).normalized;
            tangentY=Vector3.Cross(center,tangentX).normalized;
        }
        private void Update()
        {
            age+=Time.deltaTime;
            while(index<target&&age>=nextBurst)
            {
                float angle=index*2.399963f;
                float spread=.028f+.012f*(index%2);
                Vector3 side=tangentX*Mathf.Cos(angle)+tangentY*Mathf.Sin(angle);
                Vector3 direction=(center*Mathf.Cos(spread)+side*Mathf.Sin(spread)).normalized;
                effects.Burst(PlanetSurface.Point(direction,1.45f+index*.08f),
                    Palette[index%Palette.Length],22,5.4f+index*.22f);
                index++;nextBurst+=.12f;
            }
            if(index>=target&&age>.88f)Destroy(gameObject);
        }
    }
    /// <summary>Short-lived copy of a cut trail: the gap opens at the hit and
    /// erases toward both ends before the victim reappears at home.</summary>
    public sealed class TrailCutVisual : MonoBehaviour
    {
        private const float Duration=.58f;
        private Vector3[] points;
        private int split;
        private LineRenderer left,right;
        private Color color;
        private float elapsed;
        public int VisibleSegmentCount => (left!=null?left.positionCount:0)+
                                          (right!=null?right.positionCount:0);

        public static void Show(Transform parent,IReadOnlyList<Vector3> path,
            Vector3 hit,Color teamColor)
        {
            if(parent==null)return;
            var root=new GameObject("Trail cut / spreading gap");
            root.transform.SetParent(parent,false);
            var wave=new GameObject("White cut pulse");
            wave.transform.SetParent(root.transform,false);
            wave.AddComponent<PaintWave>().Configure(hit,2.0f,.54f,Color.white);
            var visual=root.AddComponent<TrailCutVisual>();
            visual.Configure(path,hit,teamColor);
        }
        private void Configure(IReadOnlyList<Vector3> path,Vector3 hit,Color teamColor)
        {
            color=teamColor;
            int count=path==null?0:path.Count;
            if(count<2)return;
            int step=Mathf.Max(1,Mathf.CeilToInt(count/280f));
            var positions=new List<Vector3>((count+step-1)/step+1);
            for(int i=0;i<count;i+=step)
                positions.Add(PlanetSurface.Point(path[i],.62f));
            if((count-1)%step!=0)
                positions.Add(PlanetSurface.Point(path[count-1],.62f));
            points=positions.ToArray();
            float closest=float.NegativeInfinity;
            for(int i=0;i<points.Length;i++)
            {
                float dot=Vector3.Dot(points[i].normalized,hit);
                if(dot>closest){closest=dot;split=i;}
            }
            left=MakeLine("Trail cut left half");
            right=MakeLine("Trail cut right half");
            Draw(0f);
            Debug.Log("TRAIL_CUT_VISUAL points="+points.Length+" split="+split);
        }
        private LineRenderer MakeLine(string title)
        {
            var line=new GameObject(title).AddComponent<LineRenderer>();
            line.transform.SetParent(transform,false);
            line.useWorldSpace=true;
            line.sharedMaterial=Colors.TrailInkMaterial();
            line.numCapVertices=6;line.numCornerVertices=4;
            return line;
        }
        private void Draw(float progress)
        {
            if(points==null)return;
            int lastLeft=Mathf.FloorToInt((split+1)*(1f-progress))-1;
            int firstRight=split+Mathf.CeilToInt((points.Length-split)*progress);
            Color fading=new Color(color.r,color.g,color.b,1f-progress);
            float width=Mathf.Lerp(.42f,.10f,progress);
            left.startColor=left.endColor=fading;
            right.startColor=right.endColor=fading;
            left.startWidth=left.endWidth=width;
            right.startWidth=right.endWidth=width;
            left.positionCount=Mathf.Max(0,lastLeft+1);
            for(int i=0;i<left.positionCount;i++)left.SetPosition(i,points[i]);
            right.positionCount=Mathf.Max(0,points.Length-firstRight);
            for(int i=0;i<right.positionCount;i++)right.SetPosition(i,points[firstRight+i]);
        }
        private void Update()
        {
            elapsed+=Time.unscaledDeltaTime;
            Draw(Mathf.Clamp01(elapsed/Duration));
            if(elapsed>=Duration)Destroy(gameObject);
        }
    }
    public sealed class PaintWave : MonoBehaviour
    {
        private float maxRadius,age,lifetime;
        private Vector3 center,tangentX,tangentY;
        private LineRenderer line;
        private Color color;
        public void Configure(Vector3 direction,float max,float duration,Color waveColor)
        {
            center=direction.normalized;
            tangentX=Vector3.Cross(center,Vector3.up).normalized;
            if(tangentX.sqrMagnitude<.1f)tangentX=Vector3.Cross(center,Vector3.forward).normalized;
            tangentY=Vector3.Cross(center,tangentX).normalized;
            maxRadius=max;lifetime=duration;color=waveColor;
            line=gameObject.AddComponent<LineRenderer>();
            line.loop=true;line.positionCount=33;
            line.sharedMaterial=Resources.Load<Material>("PlanetSprite");
            line.numCapVertices=3;line.startColor=color;line.endColor=color;
        }
        private void Update()
        {
            age+=Time.deltaTime;
            float t=Mathf.Clamp01(age/Mathf.Max(.01f,lifetime));
            float angularSize=Mathf.Lerp(.003f,maxRadius/PlanetMath.Radius,t);
            line.startWidth=Mathf.Lerp(.32f,.045f,t);line.endWidth=line.startWidth;
            Color faded=new Color(color.r,color.g,color.b,1f-t);
            line.startColor=faded;line.endColor=faded;
            for(int i=0;i<line.positionCount;i++)
            {
                float a=i*2f*Mathf.PI/(line.positionCount-1);
                Vector3 direction=(center*Mathf.Cos(angularSize)+(tangentX*Mathf.Cos(a)+tangentY*Mathf.Sin(a))*Mathf.Sin(angularSize)).normalized;
                line.SetPosition(i,PlanetSurface.Point(direction,.44f));
            }
            if(age>lifetime)Destroy(gameObject);
        }
    }
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PaintBomb : MonoBehaviour
    {
        private PlanetGame game;
        private int team;
        private Rigidbody body;
        private float seconds=2.8f;
        private float paintRadius=.095f;
        public void Launch(PlanetGame world,PlanetActor actor,float spreadDegrees=0f,float radius=.095f)
        {
            game=world;team=actor.Team;paintRadius=radius;body=GetComponent<Rigidbody>();
            body.useGravity=false;body.mass=.7f;
            Vector3 n=actor.transform.position.normalized;
            body.position=actor.transform.position+n*1.5f;
            Vector3 direction=Vector3.ProjectOnPlane(actor.transform.forward,n).normalized;
            if(direction.sqrMagnitude<.01f)
                direction=Vector3.Cross(n,Vector3.up).normalized;
            direction=Quaternion.AngleAxis(spreadDegrees,n)*direction;
            body.linearVelocity=direction*10f+n*6f;
            var trail=gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial=Resources.Load<Material>("PlanetSprite");
            trail.time=.34f;trail.minVertexDistance=.10f;trail.startWidth=.17f;trail.endWidth=0;
            trail.startColor=new Color(1,.63f,.12f,.9f);trail.endColor=new Color(1,.22f,.1f,0);trail.numCapVertices=3;
        }
        private void FixedUpdate()
        {
            if(body==null)return;
            Vector3 n=body.position.normalized;
            body.AddForce(-n*22f,ForceMode.Acceleration);
            seconds-=Time.fixedDeltaTime;
            if(seconds<=0)Explode();
        }
        private void OnCollisionEnter(Collision col)
        {
            if(col.collider.gameObject.layer==8)Explode();
        }
        private void Explode()
        {
            if(game==null)return;
            if(game.State!=MatchState.Playing){Destroy(gameObject);return;}
            Vector3 at=transform.position.normalized;
            game.ResolveBombImpact(team,at,paintRadius);
            game=null;Destroy(gameObject);
        }
        private void OnDestroy()
        {
            var renderer=GetComponent<MeshRenderer>();if(renderer!=null&&renderer.sharedMaterial!=null)Destroy(renderer.sharedMaterial);
        }
    }
}
