#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace BrainrotPlanet
{
    public sealed partial class PlanetGame
    {
        // Explicit opt-in only. Android QA: temporary app-private .network-qa
        // marker (remove after testing). The old key marker is accepted for QA
        // scripts already installed on test devices, but no longer authenticates.
        private IEnumerator Start()
        {
            if(DedicatedMatchServer)yield break;
            if(File.Exists(Path.Combine(Application.persistentDataPath,".pole-paint-qa")))
            {yield return RunPolePaintQa();yield break;}
            if(File.Exists(Path.Combine(Application.persistentDataPath,".flight-qa")))
            {yield return RunDeploymentFlightQa();yield break;}
            if(File.Exists(Path.Combine(Application.persistentDataPath,".respawn-qa")))
            {yield return RunRespawnQa();yield break;}
            if(File.Exists(Path.Combine(Application.persistentDataPath,".trail-cut-qa")))
            {yield return RunTrailCutQa();yield break;}
            if(File.Exists(Path.Combine(Application.persistentDataPath,".paint-stroke-qa")))
            {yield return RunPaintStrokeQa();yield break;}
            if(File.Exists(Path.Combine(Application.persistentDataPath,".planet-biome-qa")))
            {yield return RunPlanetBiomeQa();yield break;}
            if(File.Exists(Path.Combine(Application.persistentDataPath,".hero-menu-qa")))
            {yield return RunHeroMenuQa();yield break;}
            if(File.Exists(Path.Combine(Application.persistentDataPath,".store-qa")))
            {yield return RunStoreQa();yield break;}
            if(File.Exists(Path.Combine(Application.persistentDataPath,".wardrobe-cast-qa")))
            {yield return RunWardrobeCastQa();yield break;}
            string key=Environment.GetEnvironmentVariable("BRAINROT_QA_KEY");
            string path=Path.Combine(Application.persistentDataPath,".network-qa-key");
            if(string.IsNullOrEmpty(key) && File.Exists(path))key=File.ReadAllText(path).Trim();
            if(string.IsNullOrEmpty(key) && !File.Exists(Path.Combine(Application.persistentDataPath,".network-qa")))yield break;
            yield return RunNetworkMatchQa();
        }
        private IEnumerator RunNetworkMatchQa()
        {
            int originalHero=selectedHero,originalLast=lastBattlePlanet;
            bool originalQa=isWardrobeQa;
            var originalSkins=(string[])equippedSkins.Clone();
            try
            {
                isWardrobeQa=true;
                Application.runInBackground=true;
                yield return null;
                float catalogDeadline=Time.unscaledTime+40f;
                while(skinCatalog!=null&&skinCatalog.Busy&&Time.unscaledTime<catalogDeadline)yield return null;
                string skinPath=Path.Combine(Application.persistentDataPath,".skin-qa-selection");
                selectedHero=1;
                string qaSkin=Environment.GetEnvironmentVariable("BRAINROT_QA_SKIN");
                if(string.IsNullOrEmpty(qaSkin)&&File.Exists(skinPath))qaSkin=File.ReadAllText(skinPath).Trim();
                if(!string.IsNullOrEmpty(qaSkin))
                {
                    string id=qaSkin;var entry=skinCatalog?.Entry(id);
                    if(entry!=null)
                    {
                        selectedHero=entry.hero;equippedSkin=id;skinCatalog.EnsureLoaded(id);
                        while(!skinCatalog.Available(id)&&Time.unscaledTime<catalogDeadline)yield return null;
                    }
                }
                networkPanel=true; ConnectMatch();
                float deadline=Time.unscaledTime+45f;
                while(State!=MatchState.Playing && Time.unscaledTime<deadline)yield return null;
                if(State!=MatchState.Playing){Debug.LogError("MATCH_QA_FAIL no_start");yield break;}
                uint initialEpoch=networkEpoch;
                float nextAction=0, began=Time.unscaledTime;
                int actions=0;
                bool leave=Environment.GetEnvironmentVariable("BRAINROT_QA_LEAVE")=="1";
                while(IsNetworkReplica && State==MatchState.Playing && Time.unscaledTime-began<175f)
                {
                    paused=false;
                    savedSteering=new Vector2(Mathf.Sin((Time.unscaledTime-began)*.22f),.75f).normalized;
                    autoMoving=true;
                    if(!IsLanding && Time.unscaledTime>=nextAction)
                    {nextAction=Time.unscaledTime+13f;RequestJump();UseSkill();actions++;}
                    if(leave && Time.unscaledTime-began>35f)
                    {ReturnToMenu();Debug.Log("MATCH_QA_LEFT_FOR_BOT_TAKEOVER");yield break;}
                    yield return null;
                }
                if(!IsNetworkReplica || State!=MatchState.End)
                {Debug.LogError("MATCH_QA_FAIL no_result");yield break;}
                Debug.Log("MATCH_QA_RESULT epoch="+networkEpoch+" winner="+networkWinner+" actions="+actions);
                var biomeDecor=worldRoot.GetComponentInChildren<PlanetBiomeDecor>();
                if(biomeDecor!=null&&biomeDecor.GetComponent<Renderer>().enabled)
                    throw new InvalidOperationException("Biome decor visible on victory stage");
                RequestReplay();deadline=Time.unscaledTime+35f;
                while(networkEpoch==initialEpoch && Time.unscaledTime<deadline)yield return null;
                if(networkEpoch<=initialEpoch){Debug.LogError("MATCH_QA_FAIL no_replay");yield break;}
                deadline=Time.unscaledTime+OrbitCamera.DescentDuration+5f;
                while(IsLanding&&Time.unscaledTime<deadline)yield return null;
                if(IsLanding)throw new InvalidOperationException("Network replay deployment did not land");
                ReturnToMenu();
                StartGame();
                deadline=Time.unscaledTime+OrbitCamera.DescentDuration+5f;
                while(IsLanding&&Time.unscaledTime<deadline)yield return null;
                Debug.Log(!IsNetworkMatch && State==MatchState.Playing && Player!=null
                    ?"MATCH_QA_PASS result_replay_offline":"MATCH_QA_FAIL offline");
                ReturnToMenu();
            }
            finally
            {
                ReturnToMenu();selectedHero=originalHero;lastBattlePlanet=originalLast;
                Array.Copy(originalSkins,equippedSkins,equippedSkins.Length);
                isWardrobeQa=originalQa;previewHero=-1;
            }
        }
    }
}
#endif
