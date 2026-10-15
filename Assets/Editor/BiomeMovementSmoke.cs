#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;
namespace BrainrotPlanet.Editor
{
    public static class BiomeMovementSmoke
    {
        public static void Run()
        {
            var previousMode=Physics.simulationMode;Physics.simulationMode=SimulationMode.Script;
            var directorObject=new GameObject("Inactive biome test director");directorObject.SetActive(false);
            var director=directorObject.AddComponent<PlanetGame>();
            var arenaField=typeof(PlanetGame).GetField("<Arena>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic);
            var tick=typeof(PlanetActor).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
            int routes=0;float worst=0;
            try
            {
                foreach(BattlePlanet kind in Enum.GetValues(typeof(BattlePlanet)))
                {
                    var rules=new PlanetBiomeRules(kind,66);arenaField.SetValue(director,rules);
                    var terrain=new GameObject("Biome collision surface");terrain.AddComponent<PlanetSurface>().Build(rules);
                    var probe=new GameObject("Biome route probe");var actor=probe.AddComponent<PlanetActor>();var body=probe.GetComponent<Rigidbody>();
                    body.useGravity=false;body.constraints=RigidbodyConstraints.FreezeRotation;
                    var collider=probe.AddComponent<SphereCollider>();collider.radius=ActorCollisionRules.Radius;collider.center=new Vector3(0,.05f,0);
                    typeof(PlanetActor).GetField("body",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(actor,body);
                    typeof(PlanetActor).GetField("game",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(actor,director);actor.Active=true;
                    try
                    {
                        for(int sample=0;sample<6;sample++)
                        {
                            Vector3 centre=kind==BattlePlanet.Moon?PlanetSurface.CraterDirection(sample):rules.Zones[sample];
                            PlanetBiomeRules.Basis(centre,out Vector3 east,out _);
                            Vector3 axis=Vector3.Cross(centre,east).normalized;
                            Vector3 start=Quaternion.AngleAxis(-.17f*Mathf.Rad2Deg,axis)*centre;
                            Vector3 goal=Quaternion.AngleAxis(.17f*Mathf.Rad2Deg,axis)*centre;
                            body.position=PlanetSurface.Point(start,.73f);body.rotation=Quaternion.FromToRotation(Vector3.up,start);body.linearVelocity=Vector3.zero;
                            typeof(PlanetActor).GetField("lastDriveVelocity",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(actor,Vector3.zero);
                            Physics.SyncTransforms();float idle=0,maxIdle=0;Vector3 previous=start;
                            for(int frame=0;frame<350;frame++)
                            {
                                Vector3 normal=body.position.normalized;
                                if(Vector3.Angle(normal,goal)*Mathf.Deg2Rad*PlanetMath.Radius<.4f)break;
                                actor.Wish=PlanetMath.Tangent(normal,goal);tick.Invoke(actor,null);Physics.Simulate(.02f);
                                float distance=Vector3.Angle(previous,body.position.normalized)*Mathf.Deg2Rad*PlanetMath.Radius;
                                idle=distance<.008f?idle+.02f:0;maxIdle=Mathf.Max(maxIdle,idle);previous=body.position.normalized;
                            }
                            float remaining=Vector3.Angle(body.position.normalized,goal)*Mathf.Deg2Rad*PlanetMath.Radius;
                            if(remaining>.65f||maxIdle>.75f)throw new Exception("Biome movement stalled "+kind+" route="+sample+" remaining="+remaining+" idle="+maxIdle);
                            worst=Mathf.Max(worst,maxIdle);routes++;
                        }
                    }
                    finally{UnityEngine.Object.DestroyImmediate(probe);UnityEngine.Object.DestroyImmediate(terrain);}
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(directorObject);Physics.simulationMode=previousMode;}
            Debug.Log("BIOME_MOVEMENT_SMOKE_PASS routes="+routes+" worstStationary="+worst+" planets="+PlanetBiomeRules.Count);
        }
    }
}
#endif
