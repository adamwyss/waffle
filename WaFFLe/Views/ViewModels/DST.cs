using System.Collections.Generic;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace WaFFL.Evaluation
{
    public class DST : Item
    {
        /// <summary />
        public DST(NFLPlayer p)
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
        public static IEnumerable<DST> ConvertAndInitialize(FanastySeason season)
        {
            Collection<DST> results = new Collection<DST>();

            foreach (NFLPlayer player in season.GetAll(FanastyPosition.DST))
            {
                DST dst = new DST(player);

                int points = player.Team.ESPNTeamDefense.Estimate_Points();
                int games = season.GetAllPlayers().Where(p => p.Team == player.Team).Max(p => p.GamesPlayed());

                if (games > 0)
                {
                    dst.PointsOverReplacement = (points / games) - season.ReplacementValue.DST;

                    // fudge with the number to make the scores relative to the players who have played
                    // less than 3 games in the beginning of the season.
                    double x = 1.0;
                    if (games == 1) x = 0.333;
                    if (games == 2) x = 0.666;
                    dst.PointsOverReplacement3G = (int)Math.Round(dst.PointsOverReplacement * x, 0);
                }

                dst.FanastyPoints = points;
                dst.TotalBonuses = 0;

                results.Add(dst);
            }

            return results;
        }
    }
}
