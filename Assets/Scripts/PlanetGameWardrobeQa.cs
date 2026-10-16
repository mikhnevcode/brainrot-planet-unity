#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace BrainrotPlanet
{
    public sealed partial class PlanetGame
    {
        // Opt-in device fixture. Profile rewards are suppressed and the selected
        // hero preference is restored; cosmetic selections below are in-memory only.
        private IEnumerator RunWardrobeCastQa()
        {
            isWardrobeQa=true;Application.runInBackground=true;
            int originalHero=selectedHero;bool hadHero=PlayerPrefs.HasKey("BrainrotSelectedHero");
            bool originalSetup=nicknameSetupOpen;
            int savedHero=PlayerPrefs.GetInt("BrainrotSelectedHero",0);
            var originalSkins=(string[])equippedSkins.Clone();
            string marker=Path.Combine(Application.persistentDataPath,".wardrobe-cast-qa");
            bool rolloOnly=File.Exists(marker)&&(File.ReadAllText(marker).Trim()=="rollo03"||File.ReadAllText(marker).Trim()=="rollo04"||File.ReadAllText(marker).Trim()=="rollo05"||File.ReadAllText(marker).Trim()=="rollo06"||File.ReadAllText(marker).Trim()=="rollo07");
            int heroCount=rolloOnly?1:SkinRigProfile.HeroCount;
            string folder=Path.Combine(Application.persistentDataPath,rolloOnly?"rollo-detail-qa":"cast-qa");Directory.CreateDirectory(folder);
            try
            {
                nicknameSetupOpen=false;
                float deadline=Time.unscaledTime+35f;
                while(skinCatalog.Manifest.skins.Length<130&&Time.unscaledTime<deadline)yield return null;
                if(skinCatalog.Manifest.skins.Length<130){Debug.LogError("CAST_WARDROBE_QA_FAIL catalog");yield break;}
                for(int hero=0;hero<heroCount;hero++)
                {
                    ReturnToMenu();selectedHero=hero;networkPanel=false;previewHero=-1;wardrobeOpen=true;
                    var entries=new System.Collections.Generic.List<SkinEntry>();foreach(var e in skinCatalog.Manifest.skins)if(e.hero==hero)entries.Add(e);
                    if(entries.Count!=5){Debug.LogError("CAST_WARDROBE_QA_FAIL five_skins="+hero);yield break;}
                    for(int variant=0;variant<entries.Count;variant++)
                    {
                        var candidate=entries[variant];previewSkin=candidate.id;skinCatalog.EnsureLoaded(candidate.id);deadline=Time.unscaledTime+25f;
                        while(!skinCatalog.Available(candidate.id)&&Time.unscaledTime<deadline)yield return null;
                        if(!skinCatalog.Available(candidate.id)){Debug.LogError("CAST_WARDROBE_QA_FAIL download="+candidate.id);yield break;}
                        equippedSkin=candidate.id;previewHero=-1;wardrobeScroll=new Vector2(0,Mathf.Max(0,(variant-2)*78));yield return new WaitForSecondsRealtime(.7f);
                        yield return CaptureWardrobeQa(Path.Combine(folder,"hero"+hero.ToString("D2")+"-variant"+variant+"-wardrobe.png"));
                        Debug.Log("CAST_WARDROBE_QA_PREVIEW_PASS hero="+hero+" id="+candidate.id);
                    }
                    var entry=entries[hero%5];equippedSkin=entry.id;
                    wardrobeOpen=false;StartGame();deadline=Time.unscaledTime+15f;
                    while(IsLanding&&Time.unscaledTime<deadline)yield return null;
                    if(Player==null||SkinRigProfile.Visual(Player.transform,hero)?.GetComponent<SkinVisual>()==null)
                    {Debug.LogError("CAST_WARDROBE_QA_FAIL actor="+hero);yield break;}
                    paused=false;autoMoving=true;savedSteering=new Vector2(.35f,.9f).normalized;
                    // Ground-only powers must be tested before an ordinary jump.
                    deadline=Time.unscaledTime+3;while(!Player.Grounded&&Time.unscaledTime<deadline)yield return null;
                    UseSkill();if(skillCooldown<=0){Debug.LogError("CAST_WARDROBE_QA_FAIL skill="+hero);yield break;}
                    yield return new WaitForSecondsRealtime(.2f);RequestJump();yield return new WaitForSecondsRealtime(2.1f);
                    yield return CaptureWardrobeQa(Path.Combine(folder,"hero"+hero.ToString("D2")+"-game.png"));
                    yield return new WaitForSecondsRealtime(.4f);
                    // Force a known player winner to exercise the actual stage and
                    // result path. This is a skin fixture, not a balance benchmark.
                    var owners=new byte[PlanetMath.CellCount];for(int cell=0;cell<owners.Length;cell++)owners[cell]=1;
                    Territory.ApplyNetworkOwners(owners);Finish();
                    yield return new WaitForSecondsRealtime(1.4f);
                    yield return CaptureWardrobeQa(Path.Combine(folder,"hero"+hero.ToString("D2")+"-victory.png"));
                    yield return new WaitForSecondsRealtime(.6f);
                    if(victoryActor==null||SkinRigProfile.Visual(victoryActor.transform,hero)?.GetComponent<SkinVisual>()==null)
                    {Debug.LogError("CAST_WARDROBE_QA_FAIL stage="+hero);yield break;}
                    if(Vector3.Dot(victoryActor.transform.up,Vector3.up)<.99f)
                    {Debug.LogError("CAST_WARDROBE_QA_FAIL stage_up="+hero);yield break;}
                    Debug.Log("CAST_WARDROBE_QA_HERO_PASS hero="+hero+" id="+entry.id+" preview_game_jump_skill_victory=true");
                }
                ReturnToMenu();wardrobeOpen=false;if(rolloOnly)selectedHero=characters.Length-1;StartGame();
                float aiDeadline=Time.unscaledTime+15;while(IsLanding&&Time.unscaledTime<aiDeadline)yield return null;
                var aiOwners=new byte[PlanetMath.CellCount];for(int i=0;i<aiOwners.Length;i++)aiOwners[i]=2;
                Territory.ApplyNetworkOwners(aiOwners);Finish();yield return new WaitForSecondsRealtime(1.4f);
                if(victoryActor==null||victoryActor.Team!=2||Vector3.Dot(victoryActor.transform.up,Vector3.up)<.99f||(rolloOnly&&victoryActor.Character!=0))
                {Debug.LogError("CAST_WARDROBE_QA_FAIL ai_stage");yield break;}
                yield return CaptureWardrobeQa(Path.Combine(folder,"ai-winner.png"));
                Debug.Log("ORIGINAL_AI_WINNER_QA_PASS upright=true team=2");
                Debug.Log("CAST_WARDROBE_QA_PASS heroes="+heroCount+" skins="+(heroCount*5)+" profileRewardsUnchanged=true");
            }
            finally
            {
                ReturnToMenu();nicknameSetupOpen=originalSetup;wardrobeOpen=false;selectedHero=originalHero;previewHero=-1;
                Array.Copy(originalSkins,equippedSkins,equippedSkins.Length);
                if(hadHero)PlayerPrefs.SetInt("BrainrotSelectedHero",savedHero);else PlayerPrefs.DeleteKey("BrainrotSelectedHero");
                PlayerPrefs.Save();isWardrobeQa=false;
            }
        }
        private IEnumerator CaptureWardrobeQa(string path)
        {
            yield return new WaitForEndOfFrame();
            var previous=RenderTexture.active;RenderTexture.active=null;
            var image=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
            try
            {
                image.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);image.Apply();
                File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally {RenderTexture.active=previous;Destroy(image);}
        }
    }
}
#endif
