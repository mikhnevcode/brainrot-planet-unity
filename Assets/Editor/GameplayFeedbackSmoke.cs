#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace BrainrotPlanet.Editor
{
    public static class GameplayFeedbackSmoke
    {
        [MenuItem("Brainrot Planet/Tests/Gameplay feedback smoke")]
        public static void Run()
        {
            Require(GameplayFeedback.TrailRiskFor(0,0f)==TrailRisk.None,"closed trail");
            Require(GameplayFeedback.TrailRiskFor(20,80f)==TrailRisk.Short,"short trail");
            Require(GameplayFeedback.TrailRiskFor(70,80f)==TrailRisk.Exposed,"long trail");
            Require(GameplayFeedback.TrailRiskFor(20,20f)==TrailRisk.Exposed,"near enemy");
            Require(GameplayFeedback.TrailRiskFor(150,80f)==TrailRisk.Critical,"critical length");
            Require(GameplayFeedback.TrailRiskFor(20,10f)==TrailRisk.Critical,"critical enemy");
            Require(GameplayFeedback.DirectionLabel(Vector2.zero).StartsWith("СТОП"),"stopped");
            Require(GameplayFeedback.DirectionLabel(Vector2.up)=="ВВЕРХ","up");
            Require(GameplayFeedback.DirectionLabel(Vector2.right)=="ВПРАВО","right");
            Debug.Log("GAMEPLAY_FEEDBACK_SMOKE_PASS risks=6 directions=3");
        }

        private static void Require(bool condition,string name)
        {
            if(!condition)throw new Exception("Gameplay feedback failed: "+name);
        }
    }
}
#endif
