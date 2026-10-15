using System.Collections.Generic;
using UnityEngine;
namespace BrainrotPlanet
{
    // A non-physical copy of the equipped artwork. The real actor remains on its
    // server-owned spawn; only this client-side visual travels through space.
    public sealed class DeploymentAvatar
    {
        private readonly Transform root;
        private readonly Renderer[] originals;
        private readonly bool[] originalVisibility;
        private readonly List<Transform> limbs=new List<Transform>();
        private readonly List<Quaternion> rest=new List<Quaternion>();
        private readonly List<float> signs=new List<float>();
        private readonly List<int> limbTypes=new List<int>();
        private readonly Vector3 center;
        private readonly Quaternion facing;
        private readonly bool horizontalHero;
        public readonly float Distance;
        public Transform Root=>root;
        public int LimbCount=>limbs.Count;
        public DeploymentAvatar(Transform actor,Transform parent,float aspect)
        {
            root=new GameObject("Flying equipped hero / no physics").transform;
            root.SetParent(parent,false);
            int hero=actor.GetComponent<PlanetActor>()?.Character??0;
            horizontalHero=hero==1||hero==15||hero==16||hero==25;
            foreach(Transform child in actor)
                Object.Instantiate(child.gameObject,root,false).name=child.name;
            foreach(var collider in root.GetComponentsInChildren<Collider>())collider.enabled=false;
            HeroPreviewFraming.SampleIdle(root);
            foreach(var behaviour in root.GetComponentsInChildren<Behaviour>())behaviour.enabled=false;
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())
                if(renderer.name=="Paint aura"||renderer is LineRenderer||renderer is ParticleSystemRenderer)renderer.enabled=false;
            Bounds bounds=AvatarArt.PoseBounds(root);center=bounds.center;
            Distance=Mathf.Max(7f,Mathf.Max(bounds.size.x/(Mathf.Max(.4f,aspect)*.40f),bounds.size.y/.28f)/Mathf.Tan(25.5f*Mathf.Deg2Rad));
            facing=AvatarArt.AlignFacing(AvatarArt.LocalFacing(root),Vector3.forward);
            foreach(var bone in root.GetComponentsInChildren<Transform>(true))
            {
                string name=bone.name;int separator=name.LastIndexOf('|');if(separator>=0)name=name.Substring(separator+1);
                int suffix=name.IndexOf('.');if(suffix>=0)name=name.Substring(0,suffix);
                if(name=="Arm_L"||name=="Arm_R"||name=="Wing_L"||name=="Wing_R"||
                   name=="Fin_L"||name=="Fin_R"||name=="Leg_L"||name=="Leg_R"||
                   name=="Thigh_L"||name=="Thigh_R"||name=="Leg_C"||
                   name=="Flap_L"||name=="Flap_R"||name=="Prop_L"||name=="Prop_R")
                {
                    limbs.Add(bone);rest.Add(bone.localRotation);signs.Add(name.EndsWith("_R")?-1:1);
                    limbTypes.Add(name.StartsWith("Prop")?2:name.StartsWith("Leg")||name.StartsWith("Thigh")?1:0);
                }
            }
            originals=actor.GetComponentsInChildren<Renderer>();originalVisibility=new bool[originals.Length];
            for(int i=0;i<originals.Length;i++){originalVisibility[i]=originals[i].enabled;originals[i].enabled=false;}
        }
        public void Pose(float seconds,Transform camera,Vector3 landing,Quaternion landedRotation)
        {
            // Keep a readable hero ahead of the camera until it is close enough
            // to the ground, then transfer the visual to the real landing spot.
            const float settle=.85f;
            float land=DeploymentFlight.Ease((seconds-(DeploymentFlight.Duration-settle))/settle);
            float approach=Mathf.Clamp01(seconds/DeploymentFlight.TouchdownStart);
            // Stand-up rigs lean into a belly-down glide. Aircraft and animals
            // with naturally horizontal bodies bank rather than tipping nose-down.
            Quaternion glide=Quaternion.Euler((horizontalHero?8f:43f)+7f*Mathf.Sin(seconds*2.6f),
                9f*Mathf.Sin(seconds*1.8f),12f*Mathf.Sin(seconds*2.4f));
            Quaternion flight=Quaternion.LookRotation(camera.forward+camera.up*.16f,camera.up)*glide*facing;
            Quaternion rotation=Quaternion.Slerp(flight,landedRotation,land);
            // The hero dives and banks within the frame as well as travelling
            // through the world, instead of being a motionless camera ornament.
            float depth=Distance*Mathf.Lerp(1.22f,1f,DeploymentFlight.Ease(approach));
            float side=Mathf.Lerp(-.055f,.035f,approach)+.02f*Mathf.Sin(seconds*2.2f);
            float height=.07f-.17f*Mathf.Sin(approach*Mathf.PI*.8f);
            Vector3 flightCenter=camera.position+camera.forward*depth+
                camera.right*(depth*side)+camera.up*(depth*height);
            Vector3 position=Vector3.Lerp(flightCenter-flight*center,landing,land);
            root.SetPositionAndRotation(position,rotation);
            for(int i=0;i<limbs.Count;i++)
            {
                float phase=seconds*6.5f+signs[i]*.9f;
                float kick=limbTypes[i]==1?34f*Mathf.Sin(phase):18f*Mathf.Sin(phase);
                float spread=limbTypes[i]==1?12f:38f+12f*Mathf.Sin(seconds*4.8f);
                if(limbTypes[i]==2)
                    limbs[i].localRotation=rest[i]*Quaternion.Euler(0,0,seconds*950f*(1-land));
                else limbs[i].localRotation=rest[i]*Quaternion.Euler(signs[i]*kick*(1-land),0,signs[i]*spread*(1-land));
            }
        }
        public void Dispose()
        {
            for(int i=0;i<originals.Length;i++)if(originals[i]!=null)originals[i].enabled=originalVisibility[i];
            if(root!=null){root.gameObject.SetActive(false);Object.Destroy(root.gameObject);}
        }
    }
}
