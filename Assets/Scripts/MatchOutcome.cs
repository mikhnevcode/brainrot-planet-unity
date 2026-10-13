namespace BrainrotPlanet
{
    /// <summary>Eliminated players cannot win on retained territory.</summary>
    public static class MatchOutcome
    {
        public static int Winner(float[] scores, int playerLives)
        {
            int firstEligible = playerLives > 0 ? 1 : 2;
            int winner = firstEligible;
            for (int team = firstEligible + 1; team <= 4; team++)
                if (scores[team] > scores[winner]) winner = team;
            return winner;
        }
    }
}
