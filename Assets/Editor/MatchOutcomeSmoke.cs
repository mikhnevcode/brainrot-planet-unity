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
            Check(scores, 0, 3); Check(scores, -1, 3);
            Check(scores, 1, 1); Check(scores, 3, 1);
            scores = new[] { 0f, 5f, 12f, 12f, 8f };
            Check(scores, 3, 2); Check(scores, 0, 2);
            Check(new[] { 0f, 0f, 0f, 0f, 0f }, 0, 2);
            Check(new[] { 0f, 0f, 0f, 0f, 0f }, 1, 1);
            Debug.Log("MATCH_OUTCOME_SMOKE_PASS cases=8 zeroLivesCannotWin=true ties=lowestEligibleTeam");
        }
        private static void Check(float[] scores, int lives, int expected)
        {
            int actual = MatchOutcome.Winner(scores, lives);
            if (actual != expected) throw new Exception("Incorrect match winner: lives=" + lives + " actual=" + actual + " expected=" + expected);
        }
    }
}
#endif
