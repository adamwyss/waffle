
using System;
using System.Linq;

namespace WaFFL.Evaluation
{
    public class ReplacementValueCalculator
    {
        public ReplacementValueCalculator()
        {

        }

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

            PositionBaseline baseline = season.ReplacementValue = new PositionBaseline();

            // to calculate the replacement point value, we stack rank the players, then
            // pick the player at the postion that a replacment could be easily gotten off of wavers.

            // example:  16 QB roster spots + 16 Flex positions, but assume only 6 of them are filled with QB's
            // example:  3 RB roster spots * 16 teams, assume very few (0) flex positions will contain a RB.

            int qbReplacePos = Math.Min(22, qb.Count() - 1);
            baseline.QB = qb.ElementAt(qbReplacePos).Average;

            int rbReplacePos = Math.Min(48, rb.Count() - 1);
            baseline.RB = rb.ElementAt(rbReplacePos).Average;

            int wrReplacePos = Math.Min(48, wr.Count() - 1);
            baseline.WR = wr.ElementAt(wrReplacePos).Average;

            int kReplacePos = Math.Min(21, k.Count() - 1);
            baseline.K = k.ElementAt(kReplacePos).Average;
        }

        private struct Reference<T>
        {
            public int Average { get; set; }
            public T Context { get; set; }
        }
    }
}
