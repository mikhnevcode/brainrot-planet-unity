using UnityEngine;
namespace BrainrotPlanet
{
    // Presentation coordinates never enter the authoritative actor snapshots.
    public static class DeploymentFlight
    {
        public const float Duration=8.4f;
        public const float TouchdownStart=6.2f;
        public static float Ease(float t){t=Mathf.Clamp01(t);return t*t*(3-2*t);}
        public static Vector3 Sun(Vector3 normal,Vector3 up,int planet)
        {
            Vector3 right=Vector3.Cross(up,normal).normalized;
            float radius=72+planet*25;
            return -right*radius-normal*radius*.42f;
        }
        public static void CameraPose(float seconds,Vector3 normal,Vector3 up,int planet,
            Vector3 landing,out Vector3 position,out Quaternion rotation)
        {
            Vector3 sun=Sun(normal,up,planet);
            Vector3 overview=sun+normal*720+up*340;
            Vector3 approach=normal*104-up*16;
            Vector3 final=landing+normal*.45f+normal*11f-up*9f;
            Vector3 focus;
            if(seconds<1.6f)
            {
                float t=Ease(seconds/1.6f);
                position=overview+Vector3.Cross(up,normal)*(-12*t);
                focus=Vector3.Lerp(sun,Vector3.zero,t*.18f);
            }
            else if(seconds<5.5f)
            {
                float t=Ease((seconds-1.6f)/3.9f);
                position=Vector3.Lerp(overview-Vector3.Cross(up,normal)*12,approach,t);
                focus=Vector3.Lerp(sun*.82f,landing,t);
            }
            else
            {
                float t=Ease((seconds-5.5f)/(Duration-5.5f));
                position=Vector3.Lerp(approach,final,t);
                focus=Vector3.Lerp(landing,landing+normal*.45f,t);
            }
            rotation=Quaternion.LookRotation(focus-position,up);
        }
    }
}
