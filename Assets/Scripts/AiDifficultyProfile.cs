using UnityEngine;

namespace BrainrotPlanet
{
    public enum AiDifficulty { Easy=0, Medium=1, Hard=2 }

    /// <summary>Difficulty affects decision quality and timing, never physics or paint rules.</summary>
    public struct AiDifficultyProfile
    {
        public AiDifficulty Level;
        public float DecisionMin,DecisionMax;
        public float FirstSkillMin,FirstSkillMax,SkillMin,SkillMax;
        public float SkillConfidence,TrailAggression;
        public float HuntDegrees,DefendDegrees,ThreatDegrees,DangerDegrees;
        public float ReturnDegrees,CommitSeconds;
        public int ReturnTrailCells,RiskTrailCells;
        public float LoopFirstMin,LoopFirstMax,LoopSecondMin,LoopSecondMax;
        public float JumpPerFrame;

        public static AiDifficultyProfile For(AiDifficulty level)
        {
            switch(level)
            {
                case AiDifficulty.Easy:
                    return new AiDifficultyProfile {
                        Level=level,DecisionMin=1.15f,DecisionMax=1.65f,
                        FirstSkillMin=11f,FirstSkillMax=15f,
                        SkillMin=22f,SkillMax=29f,
                        SkillConfidence=.42f,TrailAggression=.30f,
                        HuntDegrees=17f,DefendDegrees=19f,
                        ThreatDegrees=6.5f,DangerDegrees=17f,
                        ReturnDegrees=28f,ReturnTrailCells=95,RiskTrailCells=12,
                        CommitSeconds=2.3f,
                        LoopFirstMin=.18f,LoopFirstMax=.29f,
                        LoopSecondMin=.17f,LoopSecondMax=.28f,
                        JumpPerFrame=.0005f
                    };
                case AiDifficulty.Hard:
                    return new AiDifficultyProfile {
                        Level=level,DecisionMin=.27f,DecisionMax=.43f,
                        FirstSkillMin=3.5f,FirstSkillMax=6f,
                        SkillMin=12.5f,SkillMax=14.2f,
                        SkillConfidence=.98f,TrailAggression=.98f,
                        HuntDegrees=38f,DefendDegrees=29f,
                        ThreatDegrees=13f,DangerDegrees=13f,
                        ReturnDegrees=44f,ReturnTrailCells=185,RiskTrailCells=72,
                        CommitSeconds=.72f,
                        LoopFirstMin=.29f,LoopFirstMax=.43f,
                        LoopSecondMin=.30f,LoopSecondMax=.43f,
                        JumpPerFrame=.0012f
                    };
                default:
                    return new AiDifficultyProfile {
                        Level=AiDifficulty.Medium,
                        DecisionMin=.45f,DecisionMax=.78f,
                        FirstSkillMin=4.5f,FirstSkillMax=8f,
                        SkillMin=14f,SkillMax=18f,
                        SkillConfidence=.82f,TrailAggression=.78f,
                        HuntDegrees=27f,DefendDegrees=23f,
                        ThreatDegrees=10f,DangerDegrees=13f,
                        ReturnDegrees=36f,ReturnTrailCells=145,RiskTrailCells=42,
                        CommitSeconds=1.25f,
                        LoopFirstMin=.23f,LoopFirstMax=.37f,
                        LoopSecondMin=.24f,LoopSecondMax=.39f,
                        JumpPerFrame=.0011f
                    };
            }
        }
    }
}
