#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BrainrotPlanet
{
    public sealed partial class PlanetGame
    {
        private IEnumerator RunPaintStrokeQa()
        {
            int originalHero=selectedHero,originalLast=lastBattlePlanet;
            bool originalQa=isWardrobeQa;
            string folder=Path.Combine(Application.persistentDataPath,"paint-stroke-qa");
            Directory.CreateDirectory(folder);
            try
            {
                isWardrobeQa=true;selectedHero=0;
                StartGame();
                float deadline=Time.unscaledTime+OrbitCamera.DescentDuration+6f;
                while(IsLanding&&Time.unscaledTime<deadline)yield return null;
                if(IsLanding||Player==null)throw new Exception("Paint stroke QA landing failed");
                Vector3 home=Player.SpawnDirection;
                Vector3 east=Vector3.Cross(Vector3.up,home).normalized;
                if(east.sqrMagnitude<.01f)east=Vector3.Cross(Vector3.forward,home).normalized;
                Vector3 north=Vector3.Cross(home,east).normalized;
                Vector3 At(float x,float y)=>(home+east*x+north*y).normalized;
                Territory.TraceStep(1,home);
                Territory.TraceStep(1,At(.30f,0));
                Territory.TraceStep(1,At(.35f,.19f));
                Territory.TraceStep(1,At(.23f,.34f));
                Territory.TraceStep(1,At(0,.30f));
                if(!Territory.HasTrail(1))throw new Exception("Paint stroke QA has no open trail");
                RenderPaths();
                yield return CaptureWardrobeQa(Path.Combine(folder,"open-trail.png"));
                Territory.TraceStep(1,home);
                if(Territory.HasTrail(1))throw new Exception("Paint stroke QA trail did not close");
                RenderPaths();
                var surface=worldRoot.GetComponentInChildren<PlanetSurface>();
                if(surface==null||surface.GetComponentsInChildren<CapturedPaintStroke>().Length!=1)
                    throw new Exception("Paint stroke QA local bridge not created");
                var localStroke=surface.GetComponentInChildren<CapturedPaintStroke>();
                if(surface.StrokePixelCount<10)
                    throw new Exception("Paint stroke QA path was not baked into the paint map");
                yield return CaptureWardrobeQa(Path.Combine(folder,"capture-bridge.png"));
                yield return new WaitForSecondsRealtime(4.5f);
                if(localStroke!=null||surface.StrokePixelCount<10)
                    throw new Exception("Paint stroke QA did not preserve settled ink after bridge fade");
                yield return CaptureWardrobeQa(Path.Combine(folder,"settled-paint.png"));

                // Exercise the replica's old-path/new-ownership/empty-path
                // transition without needing a second phone in this visual QA.
                var replicaPath=new List<Vector3>{At(-.05f,0),At(-.10f,.05f),At(-.15f,.08f)};
                int existingBridges=surface.GetComponentsInChildren<CapturedPaintStroke>().Length;
                Territory.ApplyNetworkTrail(2,replicaPath);
                var owners=new byte[PlanetMath.CellCount];Territory.CopyOwners(owners);
                int changedCell=PlanetMath.ToCell(home);
                owners[changedCell]=2;
                Territory.ApplyNetworkOwners(owners);
                Territory.ApplyNetworkTrail(2,Array.Empty<Vector3>());
                if(surface.GetComponentsInChildren<CapturedPaintStroke>().Length<=existingBridges)
                    throw new Exception("Paint stroke QA replica bridge not created");
                Debug.Log("PAINT_STROKE_QA_PASS local=true persistentInk=true replica=true dataPreserved=true");
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
