using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace WaFFL.Evaluation
{
    /// <summary />
    public class RB : Item
    {
        /// <summary />
        public RB(NFLPlayer p)
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
        public static IEnumerable<RB> ConvertAndInitialize(FanastySeason season)
        {
            Collection<RB> results = new Collection<RB>();

            foreach (NFLPlayer player in season.GetAll(FanastyPosition.RB))
            {
                RB rb = new RB(player);

                int points = player.FanastyPoints();
                int games = player.GamesPlayed();

                if (games > 0)
                {
                    rb.PointsOverReplacement = (points / games) - season.ReplacementValue.RB;
                    rb.PointsOverReplacement3G = (player.FanastyPointsInRecentGames(3) / 3) - season.ReplacementValue.RB;
                }

                rb.FanastyPoints = points;
                rb.TotalBonuses = player.TotalBonuses();

                results.Add(rb);
            }

            return results;
        }
    }
}
