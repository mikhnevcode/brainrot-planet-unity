using UnityEngine;

namespace BrainrotPlanet
{
    /// <summary>
    /// Opening separation is an encounter-frequency control, not a speed bonus.
    /// A smaller angular distance gives the AI a shorter physical route to
    /// approach or defend against the player. Seeds never overlap.
    /// </summary>
    public static class AiBalanceTuning
    {
        public const float CoreRadiusDegrees=12.1f;

        public static float SpawnAngle(AiDifficulty level)
        {
            switch(level)
            {
                case AiDifficulty.Easy:return 53f;
                case AiDifficulty.Hard:return 33f;
                default:return 42f;
            }
        }

        public static float StartGrace(AiDifficulty level)
        {
            switch(level)
            {
                case AiDifficulty.Easy:return 19f;
                case AiDifficulty.Hard:return 8f;
                default:return 12f;
            }
        }

        public static float RespawnGrace(AiDifficulty level)
        {
            switch(level)
            {
                case AiDifficulty.Easy:return 7f;
                case AiDifficulty.Hard:return 3f;
                default:return 5f;
            }
        }

        public static bool HasSafeStart(AiDifficulty level)
        {
            float degrees=SpawnAngle(level);
            return degrees>CoreRadiusDegrees*2f+5f && degrees<60f;
        }
    }
}
