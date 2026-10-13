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
