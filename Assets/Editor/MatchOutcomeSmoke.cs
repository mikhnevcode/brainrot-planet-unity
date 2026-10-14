#if UNITY_EDITOR
using System;
using UnityEngine;

namespace BrainrotPlanet.Editor
{
    public static class MatchOutcomeSmoke
    {
        public static void Run()
        {
            var scores = new[] { 0f, 50f, 12f, 20f, 8f };
            Check(scores, 1);
            scores = new[] { 0f, 5f, 12f, 20f, 8f };
            Check(scores, 3);
            scores = new[] { 0f, 5f, 12f, 12f, 8f };
            Check(scores, 2);
            Check(new[] { 0f, 0f, 0f, 0f, 0f }, 1);
            Debug.Log("MATCH_OUTCOME_SMOKE_PASS cases=4 ties=lowestTeam noElimination=true");
        }
        private static void Check(float[] scores, int expected)
        {
            int actual = MatchOutcome.Winner(scores);
            if (actual != expected) throw new Exception("Incorrect territory winner: actual=" + actual + " expected=" + expected);
        }
    }
}
#endif
