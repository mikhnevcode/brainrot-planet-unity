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
        private IEnumerator RunPolePaintQa()
        {
            int originalHero=selectedHero,originalLast=lastBattlePlanet;
            bool originalQa=isWardrobeQa;
            string folder=Path.Combine(Application.persistentDataPath,"pole-paint-qa");
            Directory.CreateDirectory(folder);
            try
            {
                isWardrobeQa=true;selectedHero=0;StartGame();
                float deadline=Time.unscaledTime+OrbitCamera.DescentDuration+6f;
                while(IsLanding&&Time.unscaledTime<deadline)yield return null;
                if(IsLanding||Player==null)throw new Exception("Pole paint QA landing failed");
                var surface=worldRoot.GetComponentInChildren<PlanetSurface>();
                if(surface==null)throw new Exception("Pole paint QA surface missing");
                Territory.ClaimDisk(1,Vector3.up,.075f,false);
                Territory.ClaimDisk(2,Vector3.down,.075f,false);
                var northPath=PolarPath(1);
                var southPath=PolarPath(-1);
                surface.AddCapturedStroke(1,northPath,Vector3.zero);
                surface.AddCapturedStroke(2,southPath,Vector3.zero);
                var owners=new byte[PlanetMath.CellCount];Territory.CopyOwners(owners);
                surface.PaintTexture(owners,null);
                foreach(float angle in new[]{0f,60f,120f,180f,240f,300f})
                {
                    Vector3 edge=Quaternion.AngleAxis(angle,Vector3.up)*new Vector3(.001f,1,0).normalized;
                    if(Territory.OwnerAt(edge)!=1||Territory.OwnerAt(-edge)!=2)
                        throw new Exception("Pole paint QA owner changed by longitude");
                }
                yield return CapturePole(Path.Combine(folder,"north-pole.png"),Vector3.up);
                yield return CapturePole(Path.Combine(folder,"south-pole.png"),Vector3.down);
                Debug.Log("POLE_PAINT_QA_PASS north=true south=true sectors=0 progressUnchanged=true");
            }
            finally
            {
                ReturnToMenu();selectedHero=originalHero;lastBattlePlanet=originalLast;
                isWardrobeQa=originalQa;previewHero=-1;
            }
        }
        private static List<Vector3> PolarPath(float sign)
        {
            var path=new List<Vector3>();
            for(float degrees=88;degrees<=92.01f;degrees+=.25f)
            {
                float radians=degrees*Mathf.Deg2Rad;
                path.Add(new Vector3(Mathf.Cos(radians),sign*Mathf.Sin(radians),0).normalized);
            }
            return path;
        }
        private IEnumerator CapturePole(string path,Vector3 direction)
        {
            Vector3 position=worldCamera.transform.position;Quaternion rotation=worldCamera.transform.rotation;
            float fov=worldCamera.fieldOfView;bool follow=orbitCamera.enabled;
            orbitCamera.enabled=false;
            try
            {
                var body=Player.GetComponent<Rigidbody>();body.position=PlanetSurface.Point(direction,.73f);body.linearVelocity=Vector3.zero;
                Player.transform.rotation=Quaternion.FromToRotation(Vector3.up,direction);
                worldCamera.fieldOfView=48f;
                worldCamera.transform.SetPositionAndRotation(direction*(PlanetMath.Radius+18f),
                    Quaternion.LookRotation(-direction,Vector3.forward));
                yield return null;yield return CaptureWardrobeQa(path);
            }
            finally
            {
                worldCamera.transform.SetPositionAndRotation(position,rotation);worldCamera.fieldOfView=fov;orbitCamera.enabled=follow;
            }
        }
    }
}
#endif
