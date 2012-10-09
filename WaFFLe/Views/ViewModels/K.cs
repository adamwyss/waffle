using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace WaFFL.Evaluation
{
    public class K : Item
    {
        public K(NFLPlayer p)
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
        public static IEnumerable<K> ConvertAndInitialize(FanastySeason season)
        {
            Collection<K> results = new Collection<K>();

            foreach (NFLPlayer player in season.GetAll(FanastyPosition.K))
            {
                K k = new K(player);

                int points = player.FanastyPoints();
                int games = player.GamesPlayed();

                if (games > 0)
                {
                    k.PointsOverReplacement = (points / games) - season.ReplacementValue.K;
                    k.PointsOverReplacement3G = (player.FanastyPointsInRecentGames(3) / 3) - season.ReplacementValue.K;
                }

                k.FanastyPoints = points;
                k.TotalBonuses = player.TotalBonuses();

                results.Add(k);
            }

            return results;
        }

    }
}
