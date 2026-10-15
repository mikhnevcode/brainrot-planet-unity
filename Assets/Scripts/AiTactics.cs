using UnityEngine;

namespace BrainrotPlanet
{
    public enum AiObjective { Expand, Retreat, Defend, HuntTrail }

    /// <summary>Deterministic tactical priority; separate from navigation and Unity time.</summary>
    public static class AiTactics
    {
        // Backward-compatible baseline used by existing regression checks.
        public static AiObjective Choose(bool hasOwnTrail,float homeDegrees,
            int ownTrailCells,float intruderDegrees,bool intruderAtHome,
            float enemyTrailDegrees)
        {
            return Choose(hasOwnTrail,homeDegrees,ownTrailCells,
                intruderDegrees,intruderAtHome,enemyTrailDegrees,
                AiDifficultyProfile.For(AiDifficulty.Medium));
        }

        public static AiObjective Choose(bool hasOwnTrail,float homeDegrees,
            int ownTrailCells,float intruderDegrees,bool intruderAtHome,
            float enemyTrailDegrees,AiDifficultyProfile tuning)
        {
            if(hasOwnTrail && (ownTrailCells>=tuning.ReturnTrailCells ||
                homeDegrees>=tuning.ReturnDegrees ||
                intruderDegrees<tuning.DangerDegrees ||
                enemyTrailDegrees<tuning.ThreatDegrees))
                return AiObjective.Retreat;
            if(!hasOwnTrail && intruderAtHome &&
               intruderDegrees<tuning.DefendDegrees)
                return AiObjective.Defend;
            if((!hasOwnTrail || ownTrailCells<tuning.RiskTrailCells) &&
               enemyTrailDegrees<tuning.HuntDegrees)
                return AiObjective.HuntTrail;
            return AiObjective.Expand;
        }

        public static bool WantsSkill(int hero, AiObjective mode,
            bool outsideOwnLand, float nearestOpponentDegrees,
            float nearestTrailDegrees, bool grounded)
        {
            if(!OriginalHeroCatalog.Valid(hero))return false;
            switch(OriginalHeroCatalog.Mechanic(hero))
            {
                case 0: case 3: case 5: case 9:
                    return mode==AiObjective.HuntTrail ||
                           (outsideOwnLand && mode==AiObjective.Expand);
                case 1:
                    return nearestOpponentDegrees<31f || nearestTrailDegrees<22f;
                case 2: case 6: case 7: case 8:
                    return nearestOpponentDegrees<22f;
                case 4: case 11: case 12:
                    return outsideOwnLand && mode!=AiObjective.Retreat;
                case 10:
                    return grounded && outsideOwnLand &&
                           (nearestOpponentDegrees<25f || mode==AiObjective.Expand);
                default: return false;
            }
        }
    }
}
