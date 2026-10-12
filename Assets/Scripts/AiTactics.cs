using UnityEngine;

namespace BrainrotPlanet
{
    public enum AiObjective { Expand, Retreat, Defend, HuntTrail }

    /// <summary>Deterministic tactical priority; separate from navigation and Unity time.</summary>
    public static class AiTactics
    {
        public static AiObjective Choose(bool hasOwnTrail, float homeDegrees,
            int ownTrailCells, float intruderDegrees, bool intruderAtHome,
            float enemyTrailDegrees)
        {
            // Finish a vulnerable expansion before chasing anything else.
            if(hasOwnTrail && (ownTrailCells>=145 || homeDegrees>=36f ||
                intruderDegrees<13f || enemyTrailDegrees<10f))
                return AiObjective.Retreat;
            if(!hasOwnTrail && intruderAtHome && intruderDegrees<27f)
                return AiObjective.Defend;
            // Only intercept reachable tracks. Do not abandon a long open loop.
            if((!hasOwnTrail || ownTrailCells<42) && enemyTrailDegrees<27f)
                return AiObjective.HuntTrail;
            return AiObjective.Expand;
        }

        public static bool WantsSkill(int hero, AiObjective mode,
            bool outsideOwnLand, float nearestOpponentDegrees,
            float nearestTrailDegrees, bool grounded)
        {
            switch(hero)
            {
                case 0: case 3: case 5: case 9:
                    return mode==AiObjective.HuntTrail ||
                           (outsideOwnLand && mode==AiObjective.Expand);
                case 1:
                    return nearestOpponentDegrees<31f || nearestTrailDegrees<22f;
                case 2: case 6: case 7: case 8:
                    return nearestOpponentDegrees<22f;
                case 4: case 11:
                    return outsideOwnLand && mode!=AiObjective.Retreat;
                case 10:
                    return grounded && outsideOwnLand &&
                           (nearestOpponentDegrees<25f || mode==AiObjective.Expand);
                default: return false;
            }
        }
    }
}
