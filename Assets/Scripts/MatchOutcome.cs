namespace BrainrotPlanet
{
    /// <summary>Every team stays in the match; the most territory wins at time-up.</summary>
    public static class MatchOutcome
    {
        public static int Winner(float[] scores)
        {
            int winner = 1;
            for (int team = 2; team <= 4; team++)
                if (scores[team] > scores[winner]) winner = team;
            return winner;
        }
    }
}
