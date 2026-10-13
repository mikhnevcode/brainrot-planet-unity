using UnityEngine;

namespace BrainrotPlanet
{
    public sealed class PlanetEffects : MonoBehaviour
    {
        public void Burst(Vector3 position,Color color,int amount,float speed)
        {
            var go=new GameObject("Color splash particles");
            go.transform.position=position;
            var particle=go.AddComponent<ParticleSystem>();
            particle.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=particle.main;
            main.loop=false;main.duration=1f;main.startLifetime=new ParticleSystem.MinMaxCurve(.35f,1.2f);
            main.startSpeed=new ParticleSystem.MinMaxCurve(speed*.6f,speed*1.4f);
            main.startSize=new ParticleSystem.MinMaxCurve(.12f,.40f);
            main.startColor=color;main.maxParticles=Mathf.Max(64,amount+8);
            main.gravityModifier=.35f;
            var emission=particle.emission;emission.rateOverTime=0f;
            var shape=particle.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.4f;
            var renderer=particle.GetComponent<ParticleSystemRenderer>();
            renderer.material=Colors.FromResource("PlanetParticles");
            renderer.material.SetColor("_Color",color);
            particle.Play();particle.Emit(amount);
            Destroy(go,2.4f);
        }
        /// <summary>
        /// One small themed wave and burst per skill activation (not per frame).
        /// Reuses existing particle materials; unique color and radius per hero.
        /// </summary>
        public void HeroAbility(int hero,Vector3 direction,Color teamColor)
        {
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
            line.loop=true;line.positionCount=65;
            line.material=Colors.FromResource("PlanetSprite");
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
            Vector3 at=transform.position.normalized;
            game.Territory.ClaimDisk(team,at,paintRadius);
            game.Effects.Burst(PlanetSurface.Point(at,1f),Colors.Team(team),62,6f);
            game=null;Destroy(gameObject);
        }
    }
}