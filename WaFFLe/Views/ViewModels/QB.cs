using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace WaFFL.Evaluation
{
    /// <summary />
    public class QB : Item
    {
        /// <summary />
        private QB(NFLPlayer p)
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
        public static IEnumerable<QB> ConvertAndInitialize(FanastySeason season)
        {
            Collection<QB> results = new Collection<QB>();

            foreach (NFLPlayer player in season.GetAll(FanastyPosition.QB))
            {
                QB qb = new QB(player);

                int points = player.FanastyPoints();
                int games = player.GamesPlayed();

                if (games > 0)
                {
                    qb.PointsOverReplacement = (points / games) - season.ReplacementValue.QB;
                    qb.PointsOverReplacement3G = (player.FanastyPointsInRecentGames(3) / 3) - season.ReplacementValue.QB;
                }

                qb.FanastyPoints = points;
                qb.TotalBonuses = player.TotalBonuses();

                results.Add(qb);
            }

            return results;
        }
    }
}
