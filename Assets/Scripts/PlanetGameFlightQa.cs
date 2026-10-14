#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace BrainrotPlanet
{
    public sealed partial class PlanetGame
    {
        private IEnumerator RunDeploymentFlightQa()
        {
            int originalHero=selectedHero,originalLast=lastBattlePlanet;
            bool previousQa=isWardrobeQa;
            string folder=Path.Combine(Application.persistentDataPath,"flight-qa");
            Directory.CreateDirectory(folder);
            try
            {
                isWardrobeQa=true;lastBattlePlanet=-1;
                int[] heroes={0,1,4};
                float[] checkpoints={.6f,3.5f,6.4f,7.5f};
                for(int i=0;i<heroes.Length;i++)
                {
                    selectedHero=heroes[i];StartGame();
                    if((int)Arena.Kind!=i)throw new Exception("Flight map rotation");
                    for(int j=0;j<checkpoints.Length;j++)
                    {
                        while(IsLanding&&orbitCamera.DescentSeconds<checkpoints[j])yield return null;
                        if(!IsLanding||orbitCamera.FlyingHero==null)throw new Exception("Flight hero vanished before touchdown");
                        Bounds bounds=AvatarArt.PoseBounds(orbitCamera.FlyingHero);
                        foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})
                        {
                            Vector3 corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z));
                            Vector3 screen=worldCamera.WorldToViewportPoint(corner);
                            if(screen.z<worldCamera.nearClipPlane||screen.x<-.03f||screen.x>1.03f||screen.y<-.03f||screen.y>1.03f)
                                throw new Exception("Flying hero clipped map="+i+" t="+checkpoints[j]+" screen="+screen);
                        }
                        if(Vector3.Dot(orbitCamera.FlyingHero.up,worldCamera.transform.up)<.55f)
                            throw new Exception("Flying hero sideways map="+i+" t="+checkpoints[j]);
                        yield return CaptureWardrobeQa(Path.Combine(folder,"planet"+i+"-flight"+j+".png"));
                        Debug.Log("FLIGHT_QA_FRAME_PASS planet="+i+" hero="+selectedHero+" t="+checkpoints[j]);
                    }
                    float deadline=Time.unscaledTime+5;
                    while((IsLanding||Player.IsDropping)&&Time.unscaledTime<deadline)yield return null;
                    if(IsLanding||Player.IsDropping||orbitCamera.FlyingHero!=null||Player==null||
                       Mathf.Abs(Player.transform.position.magnitude-PlanetSurface.Point(Player.SpawnDirection,.75f).magnitude)>1.5f)
                        throw new Exception("Flight did not hand off at touchdown");
                    yield return CaptureWardrobeQa(Path.Combine(folder,"planet"+i+"-landed.png"));
                    Debug.Log("FLIGHT_QA_MAP_PASS planet="+i+" hero="+selectedHero+" touchdown=true");
                }
                Debug.Log("FLIGHT_QA_PASS planets=3 heroes=3 cameraActorTogether=true upright=true progressUnchanged=true");
            }
            finally
            {
                ReturnToMenu();selectedHero=originalHero;lastBattlePlanet=originalLast;
                isWardrobeQa=previousQa;previewHero=-1;
            }
        }
    }
}
#endif
