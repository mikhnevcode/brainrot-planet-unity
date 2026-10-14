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
        private readonly Vector3 center;
        private readonly Quaternion facing;
        public readonly float Distance;
        public Transform Root=>root;
        public int LimbCount=>limbs.Count;
        public DeploymentAvatar(Transform actor,Transform parent,float aspect)
        {
            root=new GameObject("Flying equipped hero / no physics").transform;
            root.SetParent(parent,false);
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
                if(name=="Arm_L"||name=="Arm_R"||name=="Wing_L"||name=="Wing_R"||
                   name=="Fin_L"||name=="Fin_R"||name=="Leg_L"||name=="Leg_R"||
                   name=="Thigh_L"||name=="Thigh_R")
                {limbs.Add(bone);rest.Add(bone.localRotation);signs.Add(name.EndsWith("_L")?1:-1);}
            }
            originals=actor.GetComponentsInChildren<Renderer>();originalVisibility=new bool[originals.Length];
            for(int i=0;i<originals.Length;i++){originalVisibility[i]=originals[i].enabled;originals[i].enabled=false;}
        }
        public void Pose(float seconds,Transform camera,Vector3 landing,Quaternion landedRotation)
        {
            float land=DeploymentFlight.Ease((seconds-DeploymentFlight.TouchdownStart)/(DeploymentFlight.Duration-DeploymentFlight.TouchdownStart));
            // Imported aircraft have no independent limbs. A gentle bank gives
            // them a readable glide while the limb animation handles characters.
            Quaternion glide=Quaternion.Euler(2.5f*Mathf.Sin(seconds*3.1f),0,
                4f*Mathf.Sin(seconds*2.4f));
            Quaternion flight=Quaternion.LookRotation(camera.forward+camera.up*.16f,camera.up)*glide*facing;
            Quaternion rotation=Quaternion.Slerp(flight,landedRotation,land);
            Vector3 flightCenter=camera.position+camera.forward*Distance-camera.up*(Distance*.10f);
            Vector3 position=Vector3.Lerp(flightCenter-flight*center,landing,land);
            root.SetPositionAndRotation(position,rotation);
            for(int i=0;i<limbs.Count;i++)
                limbs[i].localRotation=rest[i]*Quaternion.Euler(signs[i]*(22+22*Mathf.Sin(seconds*7+i*.8f))*(1-land),0,
                    signs[i]*(12+20*Mathf.Sin(seconds*9+i*.9f))*(1-land));
        }
        public void Dispose()
        {
            for(int i=0;i<originals.Length;i++)if(originals[i]!=null)originals[i].enabled=originalVisibility[i];
            if(root!=null){root.gameObject.SetActive(false);Object.Destroy(root.gameObject);}
        }
    }
}
