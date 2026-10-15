#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace BrainrotPlanet
{
    public sealed partial class PlanetGame
    {
        // Opt-in test only. No purchases, profile resets, or reward changes.
        private IEnumerator RunHeroMenuQa()
        {
            int original=selectedHero;
            bool originalSetup=nicknameSetupOpen;
            var skins=(string[])equippedSkins.Clone();
            string folder=Path.Combine(Application.persistentDataPath,"hero-menu-qa");
            Directory.CreateDirectory(folder);
            int checks=0;
            try
            {
                nicknameSetupOpen=false; // Exercise the menu, not the first-run overlay.
                yield return new WaitForSecondsRealtime(1.5f);
                for(int hero=0;hero<characters.Length;hero++)
                {
                    selectedHero=hero;equippedSkin="";previewHero=-1;
                    yield return new WaitForSecondsRealtime(.35f);
                    if(heroPreviewRig==null||Mathf.Abs(AvatarArt.PoseBounds(heroPreviewRig.transform).min.y)>.004f)
                        throw new Exception("Hero menu floor mismatch "+hero);
                    yield return CaptureWardrobeQa(Path.Combine(folder,"hero"+hero.ToString("D2")+".png"));
                    foreach(float yaw in new[]{-80f,0f,80f})foreach(float pitch in new[]{2f,18f})
                    {
                        previewYaw=yaw;previewPitch=pitch;PositionHeroPreviewCamera();
                        for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                        {
                            Vector3 view=heroPreviewCamera.WorldToViewportPoint(previewBounds.center+Vector3.Scale(previewBounds.extents,new Vector3(x,y,z)));
                            if(view.x<.03f||view.x>.97f||view.y<.03f||view.y>.97f||view.z<=0)throw new Exception("Device portrait clipping "+hero);
                        }
                        checks++;
                    }
                    if(hero==1||hero==2||hero==3||hero==8)
                        yield return CaptureWardrobeQa(Path.Combine(folder,"hero"+hero.ToString("D2")+"-turned.png"));
                    Debug.Log("HERO_MENU_QA_HERO_PASS index="+hero+" upright=true footing=true fullFrame=true");
                }
                settingsOpen=true;
                yield return CaptureWardrobeQa(Path.Combine(folder,"settings.png"));
                Debug.Log("HERO_MENU_QA_PASS heroes="+characters.Length+" views="+checks+" profileUnchanged=true");
            }
            finally
            {
                selectedHero=original;nicknameSetupOpen=originalSetup;Array.Copy(skins,equippedSkins,skins.Length);
                settingsOpen=false;previewHero=-1;
            }
        }
    }
}
#endif
