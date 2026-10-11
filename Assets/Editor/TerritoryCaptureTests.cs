#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrainrotPlanet.Editor
{
    public static class TerritoryCaptureTests
    {
        static readonly string[] Names = {"Mid-latitude", "Wrap seam", "High latitude", "Large loop", "Opposite seam"};
        public static void Run()
        {
            for(int k=0;k<Names.Length;k++)
            {
                Vector3 start = k==0 ? new Vector3(.91f,.20f,.33f).normalized :
                                k==1 ? PlanetMath.FromLonLat(.995f,.55f) :
                                k==2 ? PlanetMath.FromLonLat(.72f,.83f) :
                                k==3 ? PlanetMath.FromLonLat(.12f,.47f) :
                                       PlanetMath.FromLonLat(.005f,.52f);
                float arc = k==3 ? .62f : k==2 ? .30f : .43f;
                var capture = new TerritorySystem(null);
                capture.Seed(1,start);
                float before = capture.Percentage(1);
                int notifications=0, areaEvents=0;
                capture.OnCapture+=(team,area,pos)=>{notifications++;areaEvents+=area;};
                Vector3 east=Vector3.Cross(Vector3.up,start);
                if(east.sqrMagnitude<.001f)east=Vector3.Cross(Vector3.right,start);
                east.Normalize();
                Vector3 north=Vector3.Cross(start,east).normalized;
                var offsets=new[]{
                    new Vector2(0,0),
                    new Vector2(arc*.80f,0),
                    new Vector2(arc*.85f,arc*.75f),
                    new Vector2(0,arc*.80f),
                    new Vector2(0,0)};
                foreach(Vector2 target in offsets)
                {
                    Vector3 goal=(start+east*target.x+north*target.y).normalized;
                    if(capture.Path(1).Count==0 && target==Vector2.zero)
                        capture.TraceStep(1,start);
                    else
                    {
                        Vector3 origin= kinematicsLast;
                        if(origin.sqrMagnitude<.001f)origin=start;
                        int steps=90;
                        for(int i=1;i<=steps;i++)
                            capture.TraceStep(1,Vector3.Slerp(origin,goal,i/(float)steps));
                    }
                    kinematicsLast=goal;
                }
                kinematicsLast=Vector3.zero;
                float after=capture.Percentage(1);
                Vector3 interior=(start+east*(arc*.68f)+north*(arc*.64f)).normalized;
                bool insidePainted=capture.OwnerAt(interior)==1;
                Debug.Log("TERRITORY_SMOKE " + Names[k] + " BEFORE=" + before.ToString("F3") +
                    " AFTER=" + after.ToString("F3") + " DIFF=" + (after-before).ToString("F3") +
                    " EVENTS=" + notifications + " CLAIM_CELLS=" + areaEvents +
                    " ACTIVE_TRAIL=" + capture.Path(1).Count+" INSIDE_PAINTED="+insidePainted);
                if(after-before<.1f || notifications<1 || !insidePainted)
                    Debug.LogError("TERRITORY_TEST_FAIL " + Names[k]);
            }
            Debug.Log("TERRITORY_SMOKE_DONE");
        }
        private static Vector3 kinematicsLast;
    }
}
#endif
