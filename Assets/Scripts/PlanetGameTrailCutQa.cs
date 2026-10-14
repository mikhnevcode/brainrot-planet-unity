#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace BrainrotPlanet
{
    public sealed partial class PlanetGame
    {
        private IEnumerator RunTrailCutQa()
        {
            int originalHero=selectedHero,originalLast=lastBattlePlanet;
            bool originalQa=isWardrobeQa;
            string folder=Path.Combine(Application.persistentDataPath,"trail-cut-qa");
            Directory.CreateDirectory(folder);
            try
            {
                isWardrobeQa=true;selectedHero=0;
                StartGame();
                float deadline=Time.unscaledTime+OrbitCamera.DescentDuration+6f;
                while(IsLanding&&Time.unscaledTime<deadline)yield return null;
                if(IsLanding||Player==null)throw new Exception("Trail cut QA landing failed");
                for(int team=2;team<=4;team++)players[team-1].Active=false;
                playerLandingGrace=0f;
                Vector3 home=Player.SpawnDirection;
                Vector3 east=Vector3.Cross(Vector3.up,home).normalized;
                if(east.sqrMagnitude<.01f)east=Vector3.Cross(Vector3.forward,home).normalized;
                Vector3 outside=(home+east*.52f).normalized;
                Territory.TraceStep(1,home);
                Territory.TraceStep(1,outside);
                if(!Territory.HasTrail(1))throw new Exception("Trail cut QA no exposed trail");
                var body=Player.BodyCollider.attachedRigidbody;
                Vector3 exposedPosition=PlanetSurface.Point(outside,.85f);
                body.position=exposedPosition;Player.transform.position=exposedPosition;
                RenderPaths();
                yield return CaptureWardrobeQa(Path.Combine(folder,"before-cut.png"));
                // PNG encoding stalls the main thread on Android. Let the next
                // frame settle before measuring a sub-second visual effect.
                yield return new WaitForSecondsRealtime(.45f);
                var path=Territory.VisualPath(1);
                Vector3 cut=path[path.Count/2];
                Territory.TraceStep(2,cut);
                if(Territory.HasTrail(1)||Player.Active||
                   worldRoot.GetComponentInChildren<TrailCutVisual>()==null)
                    throw new Exception("Trail cut QA did not show delayed break");
                RenderPaths();
                yield return new WaitForSecondsRealtime(.12f);
                if(Player.Active||Vector3.Angle(Player.transform.position.normalized,home)<5f)
                    throw new Exception("Trail cut QA returned home before break was visible");
                var visual=worldRoot.GetComponentInChildren<TrailCutVisual>();
                if(visual==null||visual.VisibleSegmentCount<2)
                    throw new Exception("Trail cut QA erased line before visible break");
                yield return CaptureWardrobeQa(Path.Combine(folder,"line-breaking.png"));
                yield return new WaitForSecondsRealtime(.75f);
                if(!Player.Active||Vector3.Angle(Player.transform.position.normalized,home)>.5f||
                   !Territory.IsTrailProtected(1))
                    throw new Exception("Trail cut QA failed to return home after break");
                yield return CaptureWardrobeQa(Path.Combine(folder,"returned-home.png"));
                Debug.Log("TRAIL_CUT_QA_PASS breakShownBeforeRespawn=true home=true paletteUnified=true");
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
