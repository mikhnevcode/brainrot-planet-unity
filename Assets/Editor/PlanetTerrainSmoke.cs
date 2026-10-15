#if UNITY_EDITOR
using System;
using UnityEngine;
namespace BrainrotPlanet.Editor
{
    public static class PlanetTerrainSmoke
    {
        public static void Run()
        {
            int probes=0;float worstCollider=0;
            foreach(BattlePlanet kind in Enum.GetValues(typeof(BattlePlanet)))
            {
                foreach(int seed in new[]{1,66,975})
                {
                    var a=new PlanetTerrainProfile(kind,seed);var b=new PlanetTerrainProfile(kind,seed);
                    if(a.Mounds.Length<5||a.Bowls.Length<7)throw new Exception("Missing isolated landforms "+kind);
                    if(a.Mounds.Length!=b.Mounds.Length||a.Bowls.Length!=b.Bowls.Length)throw new Exception("Terrain seed disagreement");
                    for(int i=0;i<128;i++)
                    {
                        Vector3 n=PlanetMath.FromLonLat(i*.6180339f%1,(i+.5f)/128);
                        float h=a.Elevation(n);
                        if(float.IsNaN(h)||float.IsInfinity(h)||h<.079f||h>4.1f||h!=b.Elevation(n))throw new Exception("Invalid/non-deterministic terrain "+kind);
                        probes++;
                    }
                }
                var go=new GameObject("Seeded terrain collision probe");
                try
                {
                    var rules=new PlanetBiomeRules(kind,66);var surface=go.AddComponent<PlanetSurface>();surface.Build(rules);
                    foreach(var hill in rules.Terrain.Mounds)
                    {
                        Vector3 n=hill.Direction;
                        if(!surface.GroundCollider.Raycast(new Ray(n*(PlanetMath.Radius+8),-n),out RaycastHit hit,16))throw new Exception("Terrain ray missed "+kind);
                        float error=Mathf.Abs(hit.point.magnitude-PlanetSurface.Point(n).magnitude);
                        if(error>.16f)throw new Exception("Terrain/actor height mismatch "+kind+" "+error);
                        worstCollider=Mathf.Max(worstCollider,error);probes++;
                    }
                    if(PlanetBiomeGrain.Get(kind).mipmapCount<2)throw new Exception("Surface texture aliases at distance");
                }
                finally{UnityEngine.Object.DestroyImmediate(go);}
            }
            var old=new GameObject("Old replay surface");var current=new GameObject("Current replay surface");
            try
            {
                old.AddComponent<PlanetSurface>().Build(new PlanetBiomeRules(BattlePlanet.Frost,1));
                var rules=new PlanetBiomeRules(BattlePlanet.Crystal,975);current.AddComponent<PlanetSurface>().Build(rules);
                UnityEngine.Object.DestroyImmediate(old);old=null;
                Vector3 n=rules.Terrain.Mounds[0].Direction;
                if(Mathf.Abs(PlanetSurface.Elevation(n)-rules.Terrain.Elevation(n))>.0001f)throw new Exception("Old surface unbound new replay terrain");
            }
            finally{if(old!=null)UnityEngine.Object.DestroyImmediate(old);UnityEngine.Object.DestroyImmediate(current);}
            Debug.Log("PLANET_TERRAIN_SMOKE_PASS maps=10 probes="+probes+" worstCollider="+worstCollider+" seeded=true replayBinding=true");
        }
    }
}
#endif
