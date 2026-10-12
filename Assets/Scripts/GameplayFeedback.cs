using UnityEngine;

namespace BrainrotPlanet
{
    public enum TrailRisk { None, Short, Exposed, Critical }

    /// <summary>
    /// Pure presentation rules for feedback that must match the real movement
    /// and trail state. Kept separate so Editor smoke tests can cover the
    /// thresholds without constructing the runtime scene.
    /// </summary>
    public static class GameplayFeedback
    {
        public static TrailRisk TrailRiskFor(int trailCells,float enemyDegrees)
        {
            if(trailCells<=0)return TrailRisk.None;
            if(enemyDegrees<=11f || trailCells>=150)return TrailRisk.Critical;
            if(enemyDegrees<=23f || trailCells>=70)return TrailRisk.Exposed;
            return TrailRisk.Short;
        }

        public static string DirectionLabel(Vector2 direction)
        {
            if(direction.sqrMagnitude<.01f)return "СТОП: СДЕЛАЙ СВАЙП";
            float degrees=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg;
            if(degrees<0f)degrees+=360f;
            if(degrees<22.5f || degrees>=337.5f)return "ВПРАВО";
            if(degrees<67.5f)return "ВВЕРХ-ВПРАВО";
            if(degrees<112.5f)return "ВВЕРХ";
            if(degrees<157.5f)return "ВВЕРХ-ВЛЕВО";
            if(degrees<202.5f)return "ВЛЕВО";
            if(degrees<247.5f)return "ВНИЗ-ВЛЕВО";
            if(degrees<292.5f)return "ВНИЗ";
            return "ВНИЗ-ВПРАВО";
        }
    }
}
