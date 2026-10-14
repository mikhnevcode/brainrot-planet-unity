#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace BrainrotPlanet
{
    public sealed partial class PlanetGame
    {
        // Explicit opt-in only. Never embeds a room key in an APK or source.
        // Android QA: temporary app-private .network-qa-key file (remove after testing).
        private IEnumerator Start()
        {
            if(DedicatedMatchServer)yield break;
            if(File.Exists(Path.Combine(Application.persistentDataPath,".store-qa")))
            {yield return RunStoreQa();yield break;}
            if(File.Exists(Path.Combine(Application.persistentDataPath,".wardrobe-cast-qa")))
            {yield return RunWardrobeCastQa();yield break;}
            string key=Environment.GetEnvironmentVariable("BRAINROT_QA_KEY");
            string path=Path.Combine(Application.persistentDataPath,".network-qa-key");
            if(string.IsNullOrEmpty(key) && File.Exists(path))key=File.ReadAllText(path).Trim();
            if(string.IsNullOrEmpty(key))yield break;
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
            networkKey=key; networkPanel=true; ConnectMatch();
            float deadline=Time.unscaledTime+35f;
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
            RequestReplay();deadline=Time.unscaledTime+35f;
            while(networkEpoch==initialEpoch && Time.unscaledTime<deadline)yield return null;
            if(networkEpoch<=initialEpoch){Debug.LogError("MATCH_QA_FAIL no_replay");yield break;}
            yield return new WaitForSecondsRealtime(6);
            ReturnToMenu();
            StartGame();
            yield return new WaitForSecondsRealtime(4);
            Debug.Log(!IsNetworkMatch && State==MatchState.Playing && Player!=null
                ?"MATCH_QA_PASS result_replay_offline":"MATCH_QA_FAIL offline");
            ReturnToMenu();
        }
    }
}
#endif
