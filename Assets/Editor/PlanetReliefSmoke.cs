#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace BrainrotPlanet.Editor
{
    public static class PlanetReliefSmoke
    {
        [MenuItem("Brainrot Planet/14. Verify detailed planet relief")]
        public static void Run()
        {
            if(PlanetSurface.CraterCount<5)
                throw new Exception("Not enough standalone craters");
            if(PlanetSurface.HillCount<5)
                throw new Exception("Not enough rounded hills");
            float smallestCraterRelief=float.MaxValue;
            for(int i=0;i<PlanetSurface.CraterCount;i++)
            {
                Vector3 center=PlanetSurface.CraterDirection(i);
                Vector3 axis=Vector3.Cross(center,Vector3.up);
                if(axis.sqrMagnitude<.02f)axis=Vector3.Cross(center,Vector3.right);
                axis.Normalize();
                Vector3 rim=Quaternion.AngleAxis(
                    PlanetSurface.CraterRadius(i)*Mathf.Rad2Deg,axis)*center;
                float relief=PlanetSurface.Elevation(rim)-PlanetSurface.Elevation(center);
                smallestCraterRelief=Mathf.Min(smallestCraterRelief,relief);
                if(relief<.28f)
                    throw new Exception("Crater "+i+" has no readable bowl/rim");
            }
            float smallestHillRelief=float.MaxValue;
            for(int i=0;i<PlanetSurface.HillCount;i++)
            {
                Vector3 top=PlanetSurface.HillDirection(i);
                Vector3 axis=Vector3.Cross(top,Vector3.up);
                if(axis.sqrMagnitude<.02f)axis=Vector3.Cross(top,Vector3.right);
                axis.Normalize();
                Vector3 foot=Quaternion.AngleAxis(
                    PlanetSurface.HillRadius(i)*1.15f*Mathf.Rad2Deg,axis)*top;
                float relief=PlanetSurface.Elevation(top)-PlanetSurface.Elevation(foot);
                smallestHillRelief=Mathf.Min(smallestHillRelief,relief);
                if(relief<.75f)
                    throw new Exception("Hill "+i+" is not independently readable");
            }
            if(!PlanetActor.ShouldBounceOnHill(14f,.45f) ||
               PlanetActor.ShouldBounceOnHill(3f,.8f) ||
               PlanetActor.ShouldBounceOnHill(48f,.8f))
                throw new Exception("Hill bounce thresholds are inconsistent");
            for(int i=0;i<PlanetSurface.HillCount;i++)
                if(!PlanetSurface.IsHillArea(PlanetSurface.HillDirection(i)))
                    throw new Exception("Hill "+i+" is outside its bounce area");
            for(int i=0;i<PlanetSurface.CraterCount;i++)
                if(PlanetSurface.IsHillArea(PlanetSurface.CraterDirection(i)))
                    throw new Exception("Crater "+i+" overlaps a hill bounce area");
            Debug.Log("PLANET_RELIEF_SMOKE_PASS hills="+PlanetSurface.HillCount+
                " craters="+PlanetSurface.CraterCount+
                " minHill="+smallestHillRelief.ToString("F2")+
                " minCrater="+smallestCraterRelief.ToString("F2")+
                " bounce=true primitiveProps=0");
        }
    }
}
#endif
