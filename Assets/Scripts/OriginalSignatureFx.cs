using UnityEngine;
namespace BrainrotPlanet
{
    // Bounded, short-lived surface ribbons. Vertex arrays are allocated once,
    // never every frame; a cast cannot change physics or paint ownership here.
    public sealed class OriginalSignatureFx : MonoBehaviour
    {
        private static readonly int[] Styles={0,1,2,3,4,5,6,7,1,3,8,9,7,3,9,4,0,5,6,9,8,6,8,7,9,1};
        private const int Points=49;
        private readonly Vector3[] positions=new Vector3[Points];
        private LineRenderer[] lines;
        private Vector3 center,x,y;
        private Color primary,accent;
        private int hero,style;
        private float elapsed;
        public static void Play(PlanetEffects source,int hero,Vector3 direction,Color team)
        {
            var definition=OriginalHeroCatalog.Get(hero);
            Color color=Color.Lerp(definition.Accent,team,.38f);
            source.Burst(PlanetSurface.Point(direction,1.0f),color,24+hero%4*6,2.6f);
            var root=new GameObject(definition.Id+" signature paint FX");root.transform.SetParent(source.transform,false);
            root.AddComponent<OriginalSignatureFx>().Configure(hero,direction,color,definition.Main);
            Debug.Log("ORIGINAL_FX_PLAYED hero="+hero+" style="+Styles[hero]);
        }
        private void Configure(int index,Vector3 direction,Color color,Color secondary)
        {
            hero=index;style=Styles[index];primary=color;accent=secondary;center=direction.normalized;
            x=Vector3.Cross(center,Vector3.up).normalized;
            if(x.sqrMagnitude<.1f)x=Vector3.Cross(center,Vector3.forward).normalized;
            y=Vector3.Cross(center,x).normalized;
            int count=style==0?3:style==1?3:style==7?3:2;
            lines=new LineRenderer[count];
            for(int i=0;i<count;i++)
            {
                var line=new GameObject("Signature stroke "+i).AddComponent<LineRenderer>();
                line.transform.SetParent(transform,false);line.sharedMaterial=Resources.Load<Material>("PlanetSprite");
                line.positionCount=Points;line.useWorldSpace=true;line.numCapVertices=3;line.numCornerVertices=2;
                lines[i]=line;
            }
        }
        private void Update()
        {
            elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/1.05f);
            for(int l=0;l<lines.Length;l++)
            {
                var line=lines[l];Color color=Color.Lerp(primary,accent,l/(float)lines.Length);
                color.a=(1-t)*.88f;line.startColor=line.endColor=color;
                line.startWidth=line.endWidth=Mathf.Lerp(.21f,.025f,t);
                for(int i=0;i<Points;i++)
                {
                    float u=i/(float)(Points-1),a=u*Mathf.PI*2;
                    float radius=(.32f+t*(2.7f+hero%4*.25f))*(1-l*.18f);
                    float dx,dy,height=.49f;
                    if(style==0){dx=(u-.5f)*radius*2;dy=(l-1)*.60f+t*.65f;}
                    else if(style==1){dx=(u-.5f)*radius;dy=(l-1)*.65f+Mathf.Sin(u*Mathf.PI)*radius*.5f;}
                    else
                    {
                        if(style==2)radius*=1+.17f*Mathf.Cos(a*6);
                        if(style==4)radius*=1+.20f*Mathf.Sin(a*(hero==15?5:8));
                        if(style==5)radius*=1+.22f*Mathf.Cos(a*3);
                        if(style==6)radius*=1+.14f*Mathf.Sin(a*7+t*5);
                        if(style==8)radius*=1+.08f*Mathf.Sin(a*4-t*8);
                        if(style==9){radius*=u*.8f+.22f;a+=t*3.1f+l*Mathf.PI;}
                        if(style==3){a+=t*2;radius*=.8f;height+=Mathf.Sin(u*Mathf.PI)*t*1.2f;}
                        dx=Mathf.Cos(a)*radius;dy=Mathf.Sin(a)*radius;
                    }
                    Vector3 n=(center+(x*dx+y*dy)/PlanetMath.Radius).normalized;
                    positions[i]=PlanetSurface.Point(n,height);
                }
                line.SetPositions(positions);
            }
            if(t>=1)Destroy(gameObject);
        }
    }
}
