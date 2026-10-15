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
                // Includes horizontal, human and non-human rigs, all ten maps.
                int[] heroes={0,1,4,2,5,6,7,8,10,12,3,9,11};
                float[] checkpoints={.12f,1.2f,3.3f,5.4f};
                for(int i=0;i<heroes.Length;i++)
                {
                    selectedHero=heroes[i];StartGame();
                    if((int)Arena.Kind!=i%PlanetBiomeRules.Count)throw new Exception("Flight map rotation");
                    Vector3 firstScreen=Vector3.zero;
                    var limbs=new System.Collections.Generic.List<Transform>();
                    var poses=new System.Collections.Generic.List<Quaternion>();
                    for(int j=0;j<checkpoints.Length;j++)
                    {
                        while(IsLanding&&orbitCamera.DescentSeconds<checkpoints[j])yield return null;
                        if(!IsLanding||orbitCamera.FlyingHero==null)throw new Exception("Flight hero vanished before touchdown");
                        Bounds bounds=AvatarArt.PoseBounds(orbitCamera.FlyingHero);
                        Vector3 top=worldCamera.WorldToViewportPoint(bounds.center+worldCamera.transform.up*bounds.extents.magnitude);
                        Vector3 bottom=worldCamera.WorldToViewportPoint(bounds.center-worldCamera.transform.up*bounds.extents.magnitude);
                        if(j==3&&top.y-bottom.y<.035f)throw new Exception("Hero too small before landing");
                        Vector3 screenCenter=worldCamera.WorldToViewportPoint(bounds.center);
                        if(j==0)
                        {
                            firstScreen=screenCenter;
                            foreach(var bone in orbitCamera.FlyingHero.GetComponentsInChildren<Transform>())
                                if(bone.name.Contains("Arm_")||bone.name.Contains("Wing_")||bone.name.Contains("Fin_")||
                                   bone.name.Contains("Leg_")||bone.name.Contains("Thigh_")||bone.name.Contains("Prop_")||bone.name.Contains("Flap_"))
                                {limbs.Add(bone);poses.Add(bone.localRotation);}
                        }
                        if(j==1)
                        {
                            if(Vector2.Distance(new Vector2(firstScreen.x,firstScreen.y),new Vector2(screenCenter.x,screenCenter.y))<.02f)
                                throw new Exception("Flying hero static in frame");
                            float motion=0;for(int bone=0;bone<limbs.Count;bone++)motion=Mathf.Max(motion,Quaternion.Angle(poses[bone],limbs[bone].localRotation));
                            if(limbs.Count>0&&motion<3f)throw new Exception("Flying limbs static");
                            Debug.Log("FLIGHT_QA_MOTION_PASS hero="+selectedHero+" limbs="+limbs.Count+" angle="+motion);
                        }
                        foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})
                        {
                            Vector3 corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z));
                            Vector3 screen=worldCamera.WorldToViewportPoint(corner);
                            if(screen.z<worldCamera.nearClipPlane||screen.x<-.03f||screen.x>1.03f||screen.y<-.03f||screen.y>1.03f)
                                throw new Exception("Flying hero clipped map="+i+" t="+checkpoints[j]+" screen="+screen);
                        }
                        if(Vector3.Dot(orbitCamera.FlyingHero.up,worldCamera.transform.up)<.40f)
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
                Debug.Log("FLIGHT_QA_PASS planets="+PlanetBiomeRules.Count+" heroes="+heroes.Length+" immediate=true gliding=true upright=true progressUnchanged=true");
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
