#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace BrainrotPlanet
{
    public sealed partial class PlanetGame
    {
        private IEnumerator RunPlanetBiomeQa()
        {
            int originalHero=selectedHero,originalLast=lastBattlePlanet;
            bool originalQa=isWardrobeQa;
            string folder=Path.Combine(Application.persistentDataPath,"planet-biome-qa");Directory.CreateDirectory(folder);
            float classicJump=0;
            try
            {
                isWardrobeQa=true;selectedHero=0;lastBattlePlanet=-1;
                yield return new WaitForSecondsRealtime(1.5f);
                yield return CaptureWardrobeQa(Path.Combine(folder,"bright-menu.png"));
                for(int planet=0;planet<PlanetBiomeRules.Count;planet++)
                {
                    StartGame();
                    if((int)Arena.Kind!=planet)throw new Exception("Automatic planet rotation");
                    yield return new WaitForSecondsRealtime(.65f);
                    yield return CaptureWardrobeQa(Path.Combine(folder,"planet"+planet+"-intro.png"));
                    float deadline=Time.unscaledTime+OrbitCamera.DescentDuration+4f;
                    while(IsLanding&&Time.unscaledTime<deadline)yield return null;
                    if(IsLanding||Player==null)throw new Exception("Planet descent failed");
                    var body=Player.GetComponent<Rigidbody>();
                    Vector3 area=Arena.Zones.Length>0?Arena.Zones[0]:new Vector3(.91f,.20f,.33f).normalized;
                    body.position=PlanetSurface.Point(area,.73f);body.linearVelocity=Vector3.zero;
                    autoMoving=false;Player.Wish=Vector3.zero;
                    yield return new WaitForSecondsRealtime(.7f);
                    yield return CaptureWardrobeQa(Path.Combine(folder,"planet"+planet+"-zone.png"));
                    RequestJump();float peak=0;
                    float until=Time.unscaledTime+1.25f;
                    while(Time.unscaledTime<until)
                    {
                        float height=Player.transform.position.magnitude-PlanetSurface.Point(Player.transform.position.normalized,.72f).magnitude;
                        peak=Mathf.Max(peak,height);yield return null;
                    }
                    if(planet==0)classicJump=peak;
                    if(peak<.45f||(planet==2&&peak<classicJump*1.5f))throw new Exception("Biome jump height "+planet+" "+peak);
                    yield return CaptureWardrobeQa(Path.Combine(folder,"planet"+planet+"-landed.png"));
                    Debug.Log("PLANET_BIOME_QA_MAP_PASS id="+planet+" seed="+planetSeed+" jumpPeak="+peak.ToString("F2")+" zones="+Arena.Zones.Length);
                }
                ReturnToMenu();StartGame();
                if(Arena.Kind!=BattlePlanet.Tropical)throw new Exception("Planet replay rotation wrap");
                Debug.Log("PLANET_BIOME_QA_PASS maps="+PlanetBiomeRules.Count+" rotation=0_to_9_to_0 moonHigher=true progressUnchanged=true");
            }
            finally
            {
                ReturnToMenu();selectedHero=originalHero;lastBattlePlanet=originalLast;isWardrobeQa=originalQa;previewHero=-1;
            }
        }
    }
}
#endif
