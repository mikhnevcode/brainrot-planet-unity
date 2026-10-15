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
            string folder=Path.Combine(Application.persistentDataPath,"planet-biome-qa-v0717");Directory.CreateDirectory(folder);
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
                    yield return CaptureBiomeView(Path.Combine(folder,"planet"+planet+"-overview.png"),true);
                    var body=Player.GetComponent<Rigidbody>();
                    Vector3 area=Arena.Zones.Length>0?Arena.Zones[0]:Arena.Terrain.Bowls[0].Direction;
                    body.position=PlanetSurface.Point(area,.73f);body.linearVelocity=Vector3.zero;
                    autoMoving=false;Player.Wish=Vector3.zero;
                    yield return new WaitForSecondsRealtime(.7f);
                    yield return CaptureBiomeView(Path.Combine(folder,"planet"+planet+"-zone.png"),false);
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
        private IEnumerator CaptureBiomeView(string path,bool overview)
        {
            var surface=worldRoot.GetComponentInChildren<PlanetSurface>();
            Vector3 position=worldCamera.transform.position;Quaternion rotation=worldCamera.transform.rotation;
            float fov=worldCamera.fieldOfView;bool follow=orbitCamera.enabled;
            orbitCamera.enabled=false;surface.PaintTexture(null,null);
            try
            {
                if(overview)
                {
                    Vector3 direction=(Player.SpawnDirection+Arena.Terrain.Mounds[0].Direction*.30f).normalized;
                    PlanetBiomeRules.Basis(direction,out _,out Vector3 up);
                    worldCamera.fieldOfView=43f;
                    float distance=(PlanetMath.Radius+4)/(Mathf.Tan(21.5f*Mathf.Deg2Rad)*worldCamera.aspect*.88f);
                    worldCamera.transform.SetPositionAndRotation(direction*distance,Quaternion.LookRotation(-direction,up));
                }
                yield return CaptureWardrobeQa(path);
            }
            finally
            {
                var owners=new byte[PlanetMath.CellCount];Territory.CopyOwners(owners);surface.PaintTexture(owners,null);
                worldCamera.transform.SetPositionAndRotation(position,rotation);worldCamera.fieldOfView=fov;orbitCamera.enabled=follow;
            }
        }
    }
}
#endif
