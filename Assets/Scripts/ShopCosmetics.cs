using UnityEngine;
namespace BrainrotPlanet
{
    public static class ShopCosmetics
    {
        public static bool ValidGrant(string g)
        {
            if(g==null)return false;var p=g.Split(':');if(p.Length!=2)return false;
            if(p[0]=="skin")return SkinCatalog.ValidId(p[1]);
            switch(p[0])
            {
                case "trail":return p[1]=="bubbles"||p[1]=="petals"||p[1]=="stars";
                case "spawn":return p[1]=="ocean"||p[1]=="bloom"||p[1]=="space";
                case "dance":return p[1]=="disco"||p[1]=="robot"||p[1]=="sway";
                case "stage":return p[1]=="coral"||p[1]=="garden"||p[1]=="orbital";
                case "emote":return p[1]=="hello"||p[1]=="great"||p[1]=="again";
                case "profile":return p[1]=="coral"||p[1]=="bloom"||p[1]=="space";
                default:return false;
            }
        }
        public static Color Tint(string key)=>key=="ocean"||key=="bubbles"||key=="coral"?new Color(.2f,.82f,1f):key=="bloom"||key=="petals"||key=="garden"?new Color(1f,.45f,.7f):new Color(.64f,.5f,1f);
    }
    // Budgeted cosmetic particles. Never replaces the team-coloured danger trail.
    public sealed class CosmeticTrail : MonoBehaviour
    {
        private PlanetActor actor;private ParticleSystem particles;private Material material;
        private bool preview;
        public void Configure(PlanetActor owner,string style,bool isPreview=false)
        {
            actor=owner;preview=isPreview;material=new Material(Resources.Load<Shader>("CosmeticSpark"));
            material.SetFloat("_Style",style=="bubbles"?0:style=="petals"?1:2);
            particles=gameObject.AddComponent<ParticleSystem>();var m=particles.main;
            m.startLifetime=.55f;m.startSpeed=.12f;m.startSize=style=="bubbles"?.15f:.10f;m.maxParticles=32;m.startColor=ShopCosmetics.Tint(style);m.simulationSpace=ParticleSystemSimulationSpace.World;
            var e=particles.emission;e.rateOverTime=0;
            var shape=particles.shape;shape.enabled=false;
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;
            var size=particles.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,1,1,0));
        }
        private float tick;
        private void Update()
        {
            if(!preview&&(actor==null||!actor.Active||actor.Wish.sqrMagnitude<.02f))return;
            tick+=Time.deltaTime;if(tick>.14f){tick=0;particles.Emit(1);}
        }
        private void OnDestroy(){if(material!=null)Destroy(material);}
    }
    [DefaultExecutionOrder(100)] public sealed class CosmeticVictoryMotion : MonoBehaviour
    {
        private Vector3 rest;private Quaternion rotation;private string style;
        private Transform left,right;
        private Quaternion leftRest,rightRest;
        public void Configure(string key)
        {
            style=key;rest=transform.localPosition;rotation=transform.localRotation;
            foreach(var t in GetComponentsInChildren<Transform>(true))
            {if(t.name=="Arm_L"||t.name=="Wing_L"||t.name=="Fin_L")left=t;if(t.name=="Arm_R"||t.name=="Wing_R"||t.name=="Fin_R")right=t;}
            if(left!=null)leftRest=left.localRotation;if(right!=null)rightRest=right.localRotation;
        }
        private void LateUpdate()
        {
            float t=Time.time*5f;float wave=style=="robot"?Mathf.Round(Mathf.Sin(t)*3)/3:Mathf.Sin(t);
            transform.localPosition=rest+Vector3.up*(style=="disco"?.035f:.018f)*Mathf.Abs(wave);
            transform.localRotation=rotation*Quaternion.Euler(0,style=="disco"?wave*12:wave*5,style=="sway"?wave*5:0);
            float gesture=style=="robot"?wave*18:style=="disco"?wave*12:wave*8;
            if(left!=null)left.localRotation=leftRest*Quaternion.Euler(gesture,0,0);
            if(right!=null)right.localRotation=rightRest*Quaternion.Euler(-gesture,0,0);
        }
    }
    public sealed class CosmeticBurst : MonoBehaviour
    {
        private Material material;
        public void Configure(string style)
        {
            material=new Material(Resources.Load<Shader>("CosmeticSpark"));material.SetFloat("_Style",style=="ocean"?0:style=="bloom"?1:2);
            var p=gameObject.AddComponent<ParticleSystem>();p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var m=p.main;m.loop=false;m.playOnAwake=false;m.startLifetime=.9f;m.startSpeed=2.4f;m.startSize=.18f;m.maxParticles=48;m.startColor=ShopCosmetics.Tint(style);m.simulationSpace=ParticleSystemSimulationSpace.World;
            var e=p.emission;e.rateOverTime=0;var shape=p.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.2f;
            p.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;p.Play();p.Emit(32);Destroy(gameObject,1.2f);
        }
        private void OnDestroy(){if(material!=null)Destroy(material);}
    }
    public sealed class CosmeticStageArt : MonoBehaviour
    {
        private readonly System.Collections.Generic.List<Material> owned=new System.Collections.Generic.List<Material>();
        public void Configure(Vector3 center,string style,float radius=1f)
        {
            if(style=="orbital")
            {
                for(int ring=0;ring<2;ring++)
                {
                    var go=new GameObject("Cosmetic orbital ring");go.transform.SetParent(transform,false);var line=go.AddComponent<LineRenderer>();
                    line.useWorldSpace=false;line.loop=true;line.positionCount=48;line.startWidth=line.endWidth=.025f;line.sharedMaterial=Resources.Load<Material>("PlanetSprite");line.startColor=line.endColor=ShopCosmetics.Tint(style);
                    for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;line.SetPosition(i,center+new Vector3(Mathf.Cos(a),.1f+ring*.08f,Mathf.Sin(a))*radius*(1.1f+ring*.1f));}
                }
            }
            else
            {
                for(int i=0;i<8;i++)
                {
                    float a=i*Mathf.PI*2/8;Vector3 at=center+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius*1.12f;
                    if(style=="coral")
                    {
                        Part(PrimitiveType.Capsule,at+Vector3.up*.15f,new Vector3(.06f,.18f,.06f),Quaternion.Euler(0,0,16));
                        Part(PrimitiveType.Capsule,at+Vector3.up*.22f+Vector3.right*.04f,new Vector3(.035f,.10f,.035f),Quaternion.Euler(0,0,-40));
                    }
                    else
                    {
                        for(int petal=0;petal<5;petal++){float b=petal*Mathf.PI*2/5;Part(PrimitiveType.Sphere,at+new Vector3(Mathf.Cos(b),.04f,Mathf.Sin(b))*.09f,new Vector3(.13f,.035f,.07f),Quaternion.Euler(0,-b*Mathf.Rad2Deg,0));}
                    }
                }
            }
        }
        private void Part(PrimitiveType type,Vector3 at,Vector3 scale,Quaternion rotation)
        {
            var p=GameObject.CreatePrimitive(type);p.name="Cosmetic stage decoration";p.transform.SetParent(transform,false);p.transform.localPosition=at;p.transform.localScale=scale;p.transform.localRotation=rotation;
            var c=p.GetComponent<Collider>();c.enabled=false;if(Application.isPlaying)Destroy(c);else DestroyImmediate(c);
            var m=Colors.New(new Color(1f,.55f,.73f));owned.Add(m);p.GetComponent<Renderer>().sharedMaterial=m;
        }
        private void OnDestroy(){foreach(var m in owned)if(m!=null){if(Application.isPlaying)Destroy(m);else DestroyImmediate(m);}}
    }
}
