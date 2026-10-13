#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;

namespace BrainrotPlanet.Editor
{
    public static class TerrainMovementSmoke
    {
        public static void Run()
        {
            var mode=Physics.simulationMode;Physics.simulationMode=SimulationMode.Script;
            var terrain=new GameObject("Movement regression terrain");terrain.AddComponent<PlanetSurface>().Build();
            var probe=new GameObject("Movement regression actor");var actor=probe.AddComponent<PlanetActor>();
            var body=probe.GetComponent<Rigidbody>();body.useGravity=false;body.constraints=RigidbodyConstraints.FreezeRotation;
            var collider=probe.AddComponent<SphereCollider>();collider.radius=ActorCollisionRules.Radius;collider.center=new Vector3(0,.05f,0);
            typeof(PlanetActor).GetField("body",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(actor,body);
            var tick=typeof(PlanetActor).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
            actor.Active=true;int failed=0,total=0;float worst=0;
            try
            {
                for(int kind=0;kind<2;kind++)for(int index=0;index<(kind==0?PlanetSurface.HillCount:PlanetSurface.CraterCount);index++)for(int bearing=0;bearing<8;bearing++)
                {
                    Vector3 center=kind==0?PlanetSurface.HillDirection(index):PlanetSurface.CraterDirection(index);
                    float radius=(kind==0?PlanetSurface.HillRadius(index):PlanetSurface.CraterRadius(index))*1.35f;
                    Vector3 axis=Vector3.Cross(center,Vector3.up).normalized;axis=Quaternion.AngleAxis(bearing*45,center)*axis;
                    Vector3 start=Quaternion.AngleAxis(-radius*Mathf.Rad2Deg,axis)*center;
                    Vector3 goal=Quaternion.AngleAxis(radius*Mathf.Rad2Deg,axis)*center;
                    body.position=PlanetSurface.Point(start,.73f);body.rotation=Quaternion.FromToRotation(Vector3.up,start);body.linearVelocity=Vector3.zero;
                    typeof(PlanetActor).GetField("lastDriveVelocity",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(actor,Vector3.zero);
                    Physics.SyncTransforms();float stationary=0,maxStationary=0;Vector3 previous=start;
                    for(int frame=0;frame<350;frame++)
                    {
                        Vector3 normal=body.position.normalized;
                        if(Vector3.Angle(normal,goal)*Mathf.Deg2Rad*PlanetMath.Radius<.40f)break;
                        actor.Wish=PlanetMath.Tangent(normal,goal);tick.Invoke(actor,null);Physics.Simulate(.02f);
                        float progress=Vector3.Angle(previous,body.position.normalized)*Mathf.Deg2Rad*PlanetMath.Radius;
                        stationary=progress<.008f?stationary+.02f:0;maxStationary=Mathf.Max(maxStationary,stationary);previous=body.position.normalized;
                    }
                    float left=Vector3.Angle(body.position.normalized,goal)*Mathf.Deg2Rad*PlanetMath.Radius;
                    worst=Mathf.Max(worst,maxStationary);total++;
                    if(left>.65f||maxStationary>.75f){failed++;Debug.Log("TERRAIN_ROUTE_FAILED kind="+kind+" index="+index+" bearing="+bearing+" remaining="+left+" stationary="+maxStationary);}
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(probe);UnityEngine.Object.DestroyImmediate(terrain);Physics.simulationMode=mode;}
            Debug.Log("TERRAIN_MOVEMENT_RESULT total="+total+" failed="+failed+" worstStationary="+worst);
            if(failed>0)throw new Exception("Terrain traversal failed: "+failed+" / "+total);
            Vector3 heading=Vector3.forward;
            for(int frame=0;frame<100;frame++)
            {
                Vector3 desired=frame%2==0?Vector3.back:Vector3.forward;
                Vector3 next=PlanetAI.StableHeading(heading,desired,Vector3.up,.02f);
                if(Vector3.Angle(heading,next)>4.81f||Mathf.Abs(next.y)>.001f)
                    throw new Exception("AI heading jitter exceeds turn limit");
                heading=next;
            }
            if(!PlanetAI.GoalReached(Vector3.up,Vector3.up,.02f)||PlanetAI.GoalReached(Vector3.up,Vector3.right,.02f))
                throw new Exception("AI arrival radius is incorrect");
            Debug.Log("AI_ARRIVAL_STEERING_PASS rapidReversals=100 maxTurnPer20ms=4.8 arrival=bounded");
        }
    }
}
#endif
