#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
namespace BrainrotPlanet.Editor
{
    public static class PlanetBiomeSmoke
    {
        private static int count;
        private static void Check(bool condition,string reason){if(!condition)throw new Exception("Planet biome: "+reason);count++;}
        public static void Run()
        {
            count=0;
            int previous=-1;for(int i=0;i<30;i++){int next=PlanetBiomeRules.Next(previous);Check(next>=0&&next<=2&&next!=previous,"rotation");previous=next;}
            foreach(int seed in new[]{1,77,975,int.MaxValue})
            foreach(BattlePlanet kind in Enum.GetValues(typeof(BattlePlanet)))
            {
                var rules=new PlanetBiomeRules(kind,seed);var replica=new PlanetBiomeRules(kind,seed);
                Check(rules.Zones.Length==replica.Zones.Length,"shared zones");
                Check(rules.Gravity>=17&&rules.Gravity<=23,"bounded gravity");
                Check(rules.JumpImpulse>=6.2f&&rules.JumpImpulse<=7.4f,"bounded jump");
                Check(kind==BattlePlanet.Moon?rules.Zones.Length==0:rules.Zones.Length==18,"partial coverage");
                for(int z=0;z<rules.Zones.Length;z++)
                {
                    Check(rules.Zones[z]==replica.Zones[z],"deterministic seed");
                    Check(PlanetBiomeRules.SafeFromSpawns(rules.Zones[z]),"safe spawn");
                    Check(rules.ZoneAt(rules.Zones[z],out float weight)==z&&weight>.99f,"zone matches visual");
                    for(int other=z+1;other<rules.Zones.Length;other++)
                        Check(Vector3.Angle(rules.Zones[z],rules.Zones[other])*Mathf.Deg2Rad>PlanetBiomeRules.ZoneRadius*2,"isolated patches");
                }
                using(var reader=MatchProtocol.Reader(MatchProtocol.Packet(3,w=>MatchProtocol.WritePlanet(w,kind,seed))))
                {Check(reader.ReadByte()==3,"planet packet");MatchProtocol.ReadPlanet(reader,out var k,out int s);MatchProtocol.End(reader);Check(k==kind&&s==seed,"planet round trip");}
            }
            var tropical=new PlanetBiomeRules(BattlePlanet.Tropical,91);
            var motion=new PlanetZoneMotion();Vector3 at=tropical.Zones[0];
            float initial=motion.Tick(tropical,at,Vector3.right,true,.02f);
            Check(Mathf.Abs(initial-.8f)<.001f,"grass mild slow");
            float multiplier=0;
            for(int i=0;i<60;i++)multiplier=motion.Tick(tropical,at,Vector3.right,true,.02f);
            Check(multiplier==1,"no permanent grass trap");
            Check(motion.Tick(tropical,at,Vector3.right,false,.02f)==1,"jump escapes grass");
            Check(motion.Tick(tropical,at,Vector3.right,true,.02f)==.8f,"landing resets grass");
            var desert=new PlanetBiomeRules(BattlePlanet.Desert,91);at=desert.Zones[0];
            Check(Mathf.Abs(motion.Tick(desert,at,desert.WindDirection(0),true,.02f)-1.16f)<.001f,"tailwind");
            Check(motion.Tick(desert,at,-desert.WindDirection(0),true,.02f)==1,"headwind never traps");
            var moon=new PlanetBiomeRules(BattlePlanet.Moon,91);
            Check(moon.JumpImpulse*moon.JumpImpulse/(2*moon.Gravity)>6.2f*6.2f/(2*23f)*1.8f,"moon jump height");
            Check(2*moon.JumpImpulse/moon.Gravity>2*6.2f/23f*1.5f,"moon hang time");
            foreach(var data in new[]{new byte[]{3,1,0,0,0},new byte[]{0,0,0,0,0},new byte[]{2,255,255,255,255},new byte[]{2}})
            {
                bool rejected=false;try{using var r=MatchProtocol.Reader(data);MatchProtocol.ReadPlanet(r,out _,out _);}
                catch(InvalidDataException){rejected=true;}catch(EndOfStreamException){rejected=true;}
                Check(rejected,"invalid planet packet rejected");
            }
            foreach(BattlePlanet kind in new[]{BattlePlanet.Tropical,BattlePlanet.Desert})
            {
                var go=new GameObject("Biome visual regression");
                try
                {
                    go.AddComponent<PlanetBiomeDecor>().Build(new PlanetBiomeRules(kind,9));
                    Check(go.GetComponentsInChildren<Collider>(true).Length==0,"decor is walk through");
                    var mesh=go.GetComponent<MeshFilter>().sharedMesh;
                    Check(mesh.vertexCount<30000&&mesh.vertexCount>0,"bounded mobile mesh");
                    Check(go.GetComponent<Renderer>().sharedMaterial.shader.isSupported,"biome shader supported");
                }
                finally{UnityEngine.Object.DestroyImmediate(go);}
            }
            Debug.Log("PLANET_BIOME_SMOKE_PASS cases="+count+" planets=3 deterministic=true noBlockers=true transientGrass=true wind=true moon=true");
        }
    }
}
#endif
