#if UNITY_EDITOR
using System;
using UnityEngine;
namespace BrainrotPlanet.Editor
{
    public static class DeploymentFlightSmoke
    {
        public static void Run()
        {
            Vector3 normal=new Vector3(.91f,.20f,.33f).normalized;
            Vector3 up=Vector3.ProjectOnPlane(Vector3.up,normal).normalized;
            Vector3 landing=PlanetSurface.Point(normal,.75f);
            var go=new GameObject("Deployment framing smoke camera");
            try
            {
                var camera=go.AddComponent<Camera>();camera.aspect=720f/1600f;camera.fieldOfView=51f;
                camera.nearClipPlane=.15f;camera.farClipPlane=1000;
                for(int planet=0;planet<3;planet++)
                {
                    Vector3 sun=DeploymentFlight.Sun(normal,up,planet);
                    DeploymentFlight.CameraPose(0,normal,up,planet,landing,out Vector3 first,out Quaternion firstRotation);
                    go.transform.SetPositionAndRotation(first,firstRotation);
                    Vector3 right=Vector3.Cross(up,normal).normalized;
                    float[] angles={0,2.4f,4.4f};
                    var bodies=new Vector3[4];bodies[0]=sun;bodies[1]=Vector3.zero;
                    for(int i=0;i<2;i++)
                    {
                        int other=(planet+i+1)%3;
                        float radius=(72+other*25)*Mathf.Sqrt(1+.42f*.42f);
                        bodies[i+2]=sun+right*Mathf.Cos(angles[other])*radius+normal*Mathf.Sin(angles[other])*radius;
                    }
                    foreach(Vector3 body in bodies)
                    {
                        Vector3 point=camera.WorldToViewportPoint(body);
                        if(point.z<=0||point.x<.01f||point.x>.99f||point.y<.01f||point.y>.99f)
                            throw new Exception("Solar overview clipped planet="+planet+" body="+body+" screen="+point);
                    }
                    Vector3 previous=first;
                    for(float t=.1f;t<=DeploymentFlight.Duration+.01f;t+=.1f)
                    {
                        DeploymentFlight.CameraPose(t,normal,up,planet,landing,out Vector3 p,out Quaternion q);
                        if(float.IsNaN(p.x)||float.IsInfinity(p.x)||float.IsNaN(q.w)||float.IsInfinity(q.w)||Vector3.Distance(p,previous)>55)
                            throw new Exception("Flight camera discontinuity planet="+planet+" t="+t);
                        if(Vector3.Dot(q*Vector3.up,up)<.4f)throw new Exception("Upside-down flight camera t="+t);
                        previous=p;
                    }
                    DeploymentFlight.CameraPose(DeploymentFlight.Duration,normal,up,planet,landing,out Vector3 final,out Quaternion finalRotation);
                    if(Vector3.Dot(finalRotation*Vector3.up,normal)<.55f||
                       Vector3.Distance(final,landing+normal*.45f+normal*11f-up*9f)>.01f)
                        throw new Exception("Hero would look sideways at landing map="+planet);
                    Debug.Log("DEPLOYMENT_FLIGHT_SMOKE_MAP_PASS planet="+planet+" sun="+sun);
                }
                Debug.Log("DEPLOYMENT_FLIGHT_SMOKE_PASS maps=3 duration="+DeploymentFlight.Duration);
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
#endif
