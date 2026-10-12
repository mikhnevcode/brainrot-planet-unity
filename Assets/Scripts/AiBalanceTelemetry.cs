using UnityEngine;
using System.Collections.Generic;

namespace BrainrotPlanet
{
    /// <summary>
    /// Development-build metrics to measure actual competitive balance.
    /// No physics changes and no per-frame data collection or allocations.
    /// </summary>
    public sealed class AiBalanceTelemetry : MonoBehaviour
    {
        private PlanetGame game;
        private IReadOnlyList<PlanetActor> actors;
        private AiDifficulty difficulty;
        private float elapsed, sampleTimer, windowTimer;
        private float visibleSeconds,nearSeconds,immediateSeconds,movingSeconds;
        private float nearestSeen=180f;
        private bool wasNear;
        private int encounterStarts,playerCaptures,botCaptures,playerTrailLosses;
        private readonly int[] teamCaptures=new int[5];

        public void Configure(PlanetGame director,IReadOnlyList<PlanetActor> pawns,
                              AiDifficulty level)
        {
            game=director;actors=pawns;difficulty=level;
            Debug.Log("AI_BALANCE_START difficulty="+level+
                " spawnDeg="+AiBalanceTuning.SpawnAngle(level).ToString("F1")+
                " botSpeed=6.5 playerSpeed=6.5");
        }

        public void RecordCapture(int team)
        {
            if(team>=1&&team<=4)
            {
                teamCaptures[team]++;
                if(team==1)playerCaptures++;
                else botCaptures++;
            }
        }

        public void RecordTrailLost(int team)
        {
            if(team==1)playerTrailLosses++;
        }

        private void Update()
        {
            if(game==null||game.State!=MatchState.Playing||game.IsLanding)return;
            float dt=Time.deltaTime;
            elapsed+=dt;
            sampleTimer+=dt;
            windowTimer+=dt;
            if(sampleTimer<.50f)return;
            float window=sampleTimer;sampleTimer=0f;
            PlanetActor player=game.Player;
            if(player==null||!player.Active)return;
            bool moving=player.Wish.sqrMagnitude>.05f;
            if(moving)movingSeconds+=window;
            Vector3 position=player.transform.position.normalized;
            float nearest=180f;
            for(int i=0;i<actors.Count;i++)
            {
                var pawn=actors[i];
                if(pawn==null||pawn.Team==1||!pawn.Active)continue;
                float separation=Vector3.Angle(position,
                    pawn.transform.position.normalized);
                nearest=Mathf.Min(nearest,separation);
            }
            nearestSeen=Mathf.Min(nearestSeen,nearest);
            if(nearest<=48f)visibleSeconds+=window;
            if(nearest<=28f)nearSeconds+=window;
            if(nearest<=15f)immediateSeconds+=window;
            bool close=nearest<=28f;
            if(close&&!wasNear)
            {
                encounterStarts++;
                Debug.Log("AI_ENCOUNTER_STARTED difficulty="+difficulty+
                          " at="+elapsed.ToString("F1")+
                          " closestDeg="+nearest.ToString("F1")+
                          " playerMoving="+moving);
            }
            wasNear=close;
            if(windowTimer>=15f)
            {
                windowTimer=0f;
                Debug.Log("AI_BALANCE_WINDOW level="+difficulty+
                    " elapsed="+elapsed.ToString("F1")+
                    " nearestDeg="+nearest.ToString("F1")+
                    " nearSeconds="+nearSeconds.ToString("F1")+
                    " encounters="+encounterStarts+
                    " playerMovingSeconds="+movingSeconds.ToString("F1")+
                    " captures="+playerCaptures+"/"+botCaptures);
            }
        }

        public void Report(int winner,float[] scores)
        {
            Debug.Log("AI_BALANCE_RESULT level="+difficulty+
                " winner="+winner+
                " playerPct="+scores[1].ToString("F2")+
                " aiPct="+scores[2].ToString("F2")+","+
                  scores[3].ToString("F2")+","+
                  scores[4].ToString("F2")+
                " encounterStarts="+encounterStarts+
                " visibleSeconds="+visibleSeconds.ToString("F1")+
                " nearSeconds="+nearSeconds.ToString("F1")+
                " dangerSeconds="+immediateSeconds.ToString("F1")+
                " nearestEverDeg="+nearestSeen.ToString("F1")+
                " playerMovingSeconds="+movingSeconds.ToString("F1")+
                " captures="+playerCaptures+"/"+botCaptures+
                " trailsLost="+playerTrailLosses+
                " duration="+elapsed.ToString("F1"));
        }
    }
}
