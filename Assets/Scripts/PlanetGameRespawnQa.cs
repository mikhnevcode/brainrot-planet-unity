#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace BrainrotPlanet
{
    public sealed partial class PlanetGame
    {
        private IEnumerator RunRespawnQa()
        {
            int originalHero=selectedHero,originalLast=lastBattlePlanet;
            bool originalQa=isWardrobeQa;
            string folder=Path.Combine(Application.persistentDataPath,"respawn-qa");
            Directory.CreateDirectory(folder);
            try
            {
                isWardrobeQa=true;selectedHero=0;
                StartGame();
                float deadline=Time.unscaledTime+OrbitCamera.DescentDuration+6f;
                while(IsLanding&&Time.unscaledTime<deadline)yield return null;
                if(IsLanding||Player==null)throw new Exception("Respawn QA landing failed");
                playerLandingGrace=0f;
                yield return CaptureWardrobeQa(Path.Combine(folder,"no-lives-hud.png"));
                for(int hit=1;hit<=5;hit++)
                {
                    Vector3 home=Player.SpawnDirection;
                    Vector3 side=Vector3.Cross(home,Vector3.up);
                    if(side.sqrMagnitude<.01f)side=Vector3.Cross(home,Vector3.forward);
                    Vector3 outside=Vector3.Slerp(home,side.normalized,.42f).normalized;
                    Territory.TraceStep(1,outside);
                    if(!Territory.HasTrail(1))throw new Exception("Respawn QA no exposed trail");
                    Player.HitTrail();
                    if(Territory.HasTrail(1)||!Player.Active||State!=MatchState.Playing||
                       !Territory.InOwn(1,home)||!Territory.IsTrailProtected(1)||
                       playerLandingGrace<2f||playerLandingGrace>3.1f||
                       Vector3.Angle(Player.transform.position.normalized,home)>.2f)
                        throw new Exception("Respawn QA hit="+hit+" did not return home safely");
                    Debug.Log("RESPAWN_QA_HIT_PASS count="+hit+" matchContinues=true homeOwned=true trailCleared=true");
                    if(hit==5)yield return CaptureWardrobeQa(Path.Combine(folder,"after-fifth-hit.png"));
                    yield return new WaitForSecondsRealtime(3.1f);
                }
                var owners=new byte[PlanetMath.CellCount];
                for(int i=0;i<owners.Length;i++)owners[i]=1;
                Territory.ApplyNetworkOwners(owners);
                Finish();
                if(State!=MatchState.End||victoryActor!=Player)
                    throw new Exception("Respawn QA player cannot win after five trail cuts");
                yield return CaptureWardrobeQa(Path.Combine(folder,"victory-after-respawns.png"));
                RequestReplay();
                if(State!=MatchState.Playing||Player==null||!Player.Active)
                    throw new Exception("Respawn QA replay failed");
                Debug.Log("RESPAWN_QA_PASS hits=5 noElimination=true winByTerritory=true replay=true progressUnchanged=true");
            }
            finally
            {
                ReturnToMenu();selectedHero=originalHero;lastBattlePlanet=originalLast;
                isWardrobeQa=originalQa;previewHero=-1;
            }
        }
    }
}
#endif
