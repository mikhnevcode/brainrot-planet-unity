#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace BrainrotPlanet.Editor
{
    public static class AiDifficultySmoke
    {
        [MenuItem("Brainrot Planet/9. Verify three AI difficulties")]
        public static void Run()
        {
            AiDifficultyProfile easy=AiDifficultyProfile.For(AiDifficulty.Easy);
            AiDifficultyProfile normal=AiDifficultyProfile.For(AiDifficulty.Medium);
            AiDifficultyProfile hard=AiDifficultyProfile.For(AiDifficulty.Hard);

            if(!(easy.DecisionMin>normal.DecisionMin &&
                 normal.DecisionMin>hard.DecisionMin))
                throw new Exception("Decision timing is not monotonic");
            if(!(easy.SkillMin>normal.SkillMin &&
                 normal.SkillMin>hard.SkillMin))
                throw new Exception("Skill cooldown is not monotonic");
            if(!(easy.HuntDegrees<normal.HuntDegrees &&
                 normal.HuntDegrees<hard.HuntDegrees))
                throw new Exception("Trail pursuit range is not monotonic");
            if(!(easy.TrailAggression<normal.TrailAggression &&
                 normal.TrailAggression<hard.TrailAggression))
                throw new Exception("Trail aggression is not monotonic");
            if(!(easy.LoopFirstMax<normal.LoopFirstMax &&
                 normal.LoopFirstMax<hard.LoopFirstMax))
                throw new Exception("Loop radius is not monotonic");
            if(!(easy.SkillConfidence<normal.SkillConfidence &&
                 normal.SkillConfidence<hard.SkillConfidence))
                throw new Exception("Skill confidence is not monotonic");

            Check(AiTactics.Choose(false,30f,0,80f,false,25f,easy),
                  AiObjective.Expand,"easy will not hunt distant trail");
            Check(AiTactics.Choose(false,30f,0,80f,false,25f,normal),
                  AiObjective.HuntTrail,"medium recognizes trail");
            Check(AiTactics.Choose(false,30f,0,80f,false,35f,hard),
                  AiObjective.HuntTrail,"hard sees distant trail");
            Check(AiTactics.Choose(true,30f,110,80f,false,180f,easy),
                  AiObjective.Retreat,"easy retreats early");
            Check(AiTactics.Choose(true,30f,110,80f,false,180f,hard),
                  AiObjective.Expand,"hard can safely extend long loop");
            Check(AiTactics.Choose(false,20f,0,12f,true,180f,easy),
                  AiObjective.Defend,"easy can still defend home");
            Check(AiTactics.Choose(true,60f,230,80f,false,180f,hard),
                  AiObjective.Retreat,"hard can retreat to close long loop");

            for(int i=0;i<3;i++)
            {
                var p=AiDifficultyProfile.For((AiDifficulty)i);
                if(p.DecisionMin<=0 || p.DecisionMax<p.DecisionMin ||
                   p.SkillMax<p.SkillMin || p.TrailAggression<0 ||
                   p.TrailAggression>1 || p.SkillConfidence<0 ||
                   p.SkillConfidence>1)
                    throw new Exception("Invalid difficulty configuration "+i);
            }
            Debug.Log("AI_DIFFICULTY_SMOKE_PASS levels=3 priorityCases=7"+
                      " decisionMs="+(easy.DecisionMin*1000f).ToString("F0")+
                      "/"+(normal.DecisionMin*1000f).ToString("F0")+
                      "/"+(hard.DecisionMin*1000f).ToString("F0"));
        }

        private static void Check(AiObjective found,AiObjective expected,string message)
        {
            if(found!=expected)
                throw new Exception("Difficulty regression: "+message+
                    " expected="+expected+" actual="+found);
        }
    }
}
#endif
