using System.Collections.Generic;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace WaFFL.Evaluation
{
    public class WR : Item
    {
        /// <summary />
        public WR(NFLPlayer p)
            : base(p)
        {
        }

        /// <summary />
        public int PointsOverReplacement { get; private set; }

        /// <summary />
        public int PointsOverReplacement3G { get; private set; }

        /// <summary />
        public int FanastyPoints { get; private set; }

        /// <summary />
        public int TotalBonuses { get; private set; }

        /// <summary />
        public static IEnumerable<WR> ConvertAndInitialize(FanastySeason season)
        {
            Collection<WR> results = new Collection<WR>();

            foreach (NFLPlayer player in season.GetAll(FanastyPosition.WR))
            {
                WR wr = new WR(player);

                int points = player.FanastyPoints();
                int games = player.GamesPlayed();

                if (games > 0)
                {
                    wr.PointsOverReplacement = (points / games) - season.ReplacementValue.WR;
                    wr.PointsOverReplacement3G = (player.FanastyPointsInRecentGames(3) / 3) - season.ReplacementValue.WR;
                }

                wr.FanastyPoints = points;
                wr.TotalBonuses = player.TotalBonuses();

                results.Add(wr);
            }

            return results;
        }
    }
}
