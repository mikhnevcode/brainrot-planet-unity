#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BrainrotPlanet.Editor
{
    /// <summary>Real C# regression checks for joining a new claim to previous land.</summary>
    public static class TerritoryJoinSmoke
    {
        [MenuItem("Brainrot Planet/6. Verify territory seams")]
        public static void Run()
        {
            var territory=new TerritorySystem(null);
            var type=typeof(TerritorySystem);
            var owners=(byte[])type.GetField("owner",
                BindingFlags.NonPublic|BindingFlags.Instance).GetValue(territory);
            var repair=type.GetMethod("RepairCaptureSeams",
                BindingFlags.NonPublic|BindingFlags.Instance);
            if(repair==null)throw new Exception("Missing seam repair");

            // A narrow gap between old and new, including an enemy-owned cell.
            int w=PlanetMath.Width;
            for(int y=46;y<=53;y++)
            for(int x=12;x<=19;x++)owners[y*w+x]=1;
            byte[] old=(byte[])owners.Clone();
            for(int y=46;y<=53;y++)
            for(int x=23;x<=30;x++)owners[y*w+x]=1;
            owners[49*w+21]=2;
            int added=(int)repair.Invoke(territory,new object[]{1,old});
            for(int y=46;y<=53;y++)
            for(int x=20;x<=22;x++)
            {
                byte expected=(byte)((y==49&&x==21)?2:1);
                if(owners[y*w+x]!=expected)
                    throw new Exception("Seam not joined at "+x+","+y);
            }
            if(owners[49*w+65]!=0||added<10)
                throw new Exception("Unexpected seam expansion");

            // Longitudes 255 and 0 are neighbors on the sphere.
            Array.Clear(owners,0,owners.Length);
            for(int y=60;y<=68;y++)
            for(int x=253;x<=255;x++)owners[y*w+x]=1;
            old=(byte[])owners.Clone();
            for(int y=60;y<=68;y++)
            for(int x=2;x<=5;x++)owners[y*w+x]=1;
            repair.Invoke(territory,new object[]{1,old});
            for(int y=60;y<=68;y++)
                if(owners[y*w]!=1||owners[y*w+1]!=1)
                    throw new Exception("Longitude wrap seam at latitude "+y);

            // Widely separated land does not get automatically connected.
            Array.Clear(owners,0,owners.Length);
            for(int y=60;y<=68;y++)
                for(int x=20;x<=28;x++)owners[y*w+x]=1;
            old=(byte[])owners.Clone();
            for(int y=60;y<=68;y++)
                for(int x=49;x<=58;x++)owners[y*w+x]=1;
            if((int)repair.Invoke(territory,new object[]{1,old})!=0)
                throw new Exception("Distant areas were connected incorrectly");

            // A full actual capture from an owned seed through a closed trail.
            var capture=new TerritorySystem(null);
            Vector3 start=PlanetMath.FromLonLat(.50f,.50f);
            capture.Seed(1,start);
            float before=capture.Percentage(1);
            int events=0;
            capture.OnCapture+=(team,count,at)=>{if(team==1)events++;};
            Vector3[] path={
                PlanetMath.FromLonLat(.50f,.55f),
                PlanetMath.FromLonLat(.62f,.64f),
                PlanetMath.FromLonLat(.72f,.53f),
                PlanetMath.FromLonLat(.62f,.39f),
                PlanetMath.FromLonLat(.50f,.45f)
            };
            capture.TraceStep(1,path[0]);
            for(int i=1;i<path.Length;i++)
                for(int sub=1;sub<=12;sub++)
                    capture.TraceStep(1,Vector3.Slerp(path[i-1],path[i],sub/12f));
            float after=capture.Percentage(1);
            if(events<1 || after<=before+.10f || after>before+30f)
                throw new Exception("Capture changed unexpected area: "+
                    before.ToString("F2")+" -> "+after.ToString("F2")+
                    " events="+events);

            // A jump can coincide with a slow mobile frame, so Update may submit
            // two directions many degrees apart. The logical trail must be as
            // continuous as the LineRenderer that joins those same directions.
            var airborne=new TerritorySystem(null);
            airborne.TraceStep(1,PlanetMath.FromLonLat(.18f,.47f));
            airborne.TraceStep(1,PlanetMath.FromLonLat(.31f,.58f));
            var airbornePath=airborne.VisualPath(1);
            var airborneCells=airborne.Path(1);
            int worstCellStep=0;
            for(int i=1;i<airborneCells.Count;i++)
            {
                int a=airborneCells[i-1],b=airborneCells[i];
                int dx=Mathf.Abs(a%PlanetMath.Width-b%PlanetMath.Width);
                dx=Mathf.Min(dx,PlanetMath.Width-dx);
                int dy=Mathf.Abs(a/PlanetMath.Width-b/PlanetMath.Width);
                worstCellStep=Mathf.Max(worstCellStep,Mathf.Max(dx,dy));
            }
            if(airbornePath.Count<40||worstCellStep>1)
                throw new Exception("Airborne trail contains a hidden logical gap: count="+
                    airbornePath.Count+" worstCellStep="+worstCellStep);

            var jumpCapture=new TerritorySystem(null);
            jumpCapture.Seed(1,start);
            float jumpBefore=jumpCapture.Percentage(1);
            int jumpEvents=0;
            jumpCapture.OnCapture+=(team,count,at)=>{if(team==1)jumpEvents++;};
            for(int i=0;i<path.Length;i++)jumpCapture.TraceStep(1,path[i]);
            float jumpAfter=jumpCapture.Percentage(1);
            if(jumpEvents<1||jumpAfter<=jumpBefore+.10f||jumpAfter>jumpBefore+30f)
                throw new Exception("Sparse airborne loop did not fill: "+
                    jumpBefore.ToString("F2")+" -> "+jumpAfter.ToString("F2")+
                    " events="+jumpEvents);
            Debug.Log("TERRITORY_JOIN_SMOKE_PASS seam="+added+
                " oldPercent="+before.ToString("F2")+
                " capturedPercent="+after.ToString("F2")+
                " events="+events+" airbornePoints="+airbornePath.Count+
                " airborneWorstCellStep="+worstCellStep+
                " sparseCapture="+jumpAfter.ToString("F2"));
        }
    }
}
#endif
