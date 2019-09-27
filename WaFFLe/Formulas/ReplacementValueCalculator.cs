
using System;
using System.Linq;

namespace WaFFL.Evaluation
{
    public class ReplacementValueCalculator
    {
        public void Calculate(FanastySeason season)
        {
            NFLPlayer[] players = season.GetAllPlayers().ToArray();

            int count = players.Length;
            int middle = count / 2;

            Reference<NFLPlayer>[] tally = new Reference<NFLPlayer>[count];

            for (int i = 0; i < count; i++)
            {
                int p = players[i].FanastyPoints();
                int g = players[i].GamesPlayed();

                tally[i] = new Reference<NFLPlayer>();
                if (g > 0)
                {
                    tally[i].Average = p / g;
                }

                tally[i].Context = players[i];
            }

            var qb = from p in tally
                     where p.Context.Position == FanastyPosition.QB
                     orderby p.Average descending
                     select p;

            var rb = from p in tally
                     where p.Context.Position == FanastyPosition.RB
                     orderby p.Average descending
                     select p;

            var wr = from p in tally
                     where p.Context.Position == FanastyPosition.WR
                     orderby p.Average descending
                     select p;

            var k = from p in tally
                    where p.Context.Position == FanastyPosition.K
                    orderby p.Average descending
                    select p;

            var dst = from p in tally
                      where p.Context.Position == FanastyPosition.DST
                      orderby p.Average descending
                      select p;

            PositionBaseline baseline = season.ReplacementValue = new PositionBaseline();

            // to calculate the replacement point value, we stack rank the players, then
            // pick the player at the postion that a replacment could be easily gotten off of wavers.

            // example:  16 QB roster spots + 16 Flex positions / 3 positions that will likely filled a flex position (QB, K, or DST)
            // example:  3 RB roster spots * 16 teams, assume very few (1) flex positions will contain a RB.

            baseline.QB = CalculateReplacementValue(qb, 21);
            baseline.RB = CalculateReplacementValue(rb, 48);
            baseline.WR = CalculateReplacementValue(wr, 48);
            baseline.K = CalculateReplacementValue(k, 21);
            baseline.DST = CalculateReplacementValue(dst, 21);
        }

        private int CalculateReplacementValue(IOrderedEnumerable<Reference<NFLPlayer>> players, int suggestedReplacement)
        {
            int count = players.Count();
            if (count > 0)
            {
                int replacePos = Math.Min(suggestedReplacement, count - 1);
                return players.ElementAt(replacePos).Average;
            }

            return 0;
        }

        private struct Reference<T>
        {
            public int Average { get; set; }
            public T Context { get; set; }
        }
    }
}
