#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace BrainrotPlanet.Editor
{
    /// <summary>Deterministic AI priorities and real trail target detection.</summary>
    public static class AiTacticsSmoke
    {
        [MenuItem("Brainrot Planet/8. Verify opponent tactics")]
        public static void Run()
        {
            Check(AiTactics.Choose(true,43f,156,180f,false,180f),
                AiObjective.Retreat,"retreat long exposed trail");
            Check(AiTactics.Choose(false,10f,0,8f,true,180f),
                AiObjective.Defend,"protect core against intruder");
            Check(AiTactics.Choose(false,50f,0,60f,false,9f),
                AiObjective.HuntTrail,"pursue exposed enemy trail");
            Check(AiTactics.Choose(false,50f,0,60f,false,180f),
                AiObjective.Expand,"default expansion");
            Check(AiTactics.Choose(true,15f,50,40f,false,15f),
                AiObjective.Expand,"avoid unnecessary risky hunt");
            if(!AiTactics.WantsSkill(2,AiObjective.Defend,false,10f,180f,true))
                throw new Exception("Frigo does not slow nearby enemy");
            if(AiTactics.WantsSkill(2,AiObjective.Expand,false,110f,180f,true))
                throw new Exception("Frigo wasted ice wave with no enemies");
            if(!AiTactics.WantsSkill(0,AiObjective.HuntTrail,false,14f,6f,true))
                throw new Exception("Shark should sprint to opponent");
            if(AiTactics.WantsSkill(10,AiObjective.Expand,true,11f,5f,false))
                throw new Exception("Wheel frog must be grounded to spring");

            var territory=new TerritorySystem(null);
            var seed=PlanetMath.FromLonLat(.5f,.5f);
            territory.Seed(2,seed);
            Vector3 p1=PlanetMath.FromLonLat(.61f,.52f);
            Vector3 p2=PlanetMath.FromLonLat(.64f,.55f);
            territory.TraceStep(2,p1);
            territory.TraceStep(2,p2);
            Vector3 target;
            int foundTeam;float distance;
            bool found=territory.TryFindEnemyTrail(1,p2,35f,
                out target,out foundTeam,out distance);
            if(!found||foundTeam!=2||distance>10f)
                throw new Exception("Enemy trail search failed: "+
                    found+" team="+foundTeam+" deg="+distance);
            if(territory.TryFindEnemyTrail(2,p2,35f,out target,out foundTeam,out distance))
                throw new Exception("Bot picked its own exposed trail");
            territory.ResetTrail(2);
            if(territory.TryFindEnemyTrail(1,p2,35f,out target,out foundTeam,out distance))
                throw new Exception("Cancelled enemy trail was still targeted");

            Debug.Log("AI_TACTICS_SMOKE_PASS priorities=5 skills=4 trailTarget=2 reset=OK");
        }
        private static void Check(AiObjective actual,AiObjective expected,string reason)
        {
            if(actual!=expected)
                throw new Exception("AI priority "+reason+" expected="+expected+
                                    " actual="+actual);
        }
    }
}
#endif
