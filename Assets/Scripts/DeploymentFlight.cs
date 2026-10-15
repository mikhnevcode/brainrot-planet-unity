using UnityEngine;
namespace BrainrotPlanet
{
    // Presentation coordinates never enter the authoritative actor snapshots.
    public static class DeploymentFlight
    {
        public const float Duration=6.2f;
        public const float TouchdownStart=4f;
        public static float Ease(float t){t=Mathf.Clamp01(t);return t*t*(3-2*t);}
        public static float OrbitRadius(int planet)=>54+planet*13;
        public static float OrbitAngle(int planet)=>planet*2.399963f;
        public static Vector3 OrbitOffset(Vector3 normal,Vector3 up,int planet,float seconds=0)
        {
            Vector3 right=Vector3.Cross(up,normal).normalized;
            float angle=OrbitAngle(planet)+seconds*(.025f/(planet+1));
            return (right*Mathf.Cos(angle)+normal*Mathf.Sin(angle))*OrbitRadius(planet);
        }
        public static Vector3 Sun(Vector3 normal,Vector3 up,int planet)
        {
            // The selected, walkable planet at origin lies on its real orbit.
            return -OrbitOffset(normal,up,planet);
        }
        public static void CameraPose(float seconds,Vector3 normal,Vector3 up,int planet,
            Vector3 landing,out Vector3 position,out Quaternion rotation)
        {
            Vector3 sun=Sun(normal,up,planet);
            Vector3 overview=sun+normal*920+up*230;
            Vector3 approach=normal*104-up*16;
            Vector3 final=landing+normal*.45f+normal*11f-up*9f;
            Vector3 focus;
            if(seconds<TouchdownStart)
            {
                float t=Mathf.Clamp01(seconds/TouchdownStart);
                // Nonzero velocity at the first frame: no overview hold.
                float travel=1-(1-t)*(1-t);
                position=Vector3.Lerp(overview,approach,travel);
                focus=Vector3.Lerp(sun,landing,Ease(t));
            }
            else
            {
                float t=Ease((seconds-TouchdownStart)/(Duration-TouchdownStart));
                position=Vector3.Lerp(approach,final,t);
                focus=Vector3.Lerp(landing,landing+normal*.45f,t);
            }
            rotation=Quaternion.LookRotation(focus-position,up);
        }
    }
}
