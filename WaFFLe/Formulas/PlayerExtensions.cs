using System;
using System.Collections.Generic;
using System.Linq;

namespace WaFFL.Evaluation
{
    /// <summary />
    public static class PlayerExtensions
    {
        /// <summary />
        public static int GamesPlayed(this NFLPlayer p)
        {
            // count all games that the player played in
            return p.GameLog.Count(g => !g.IsDNP());
        }

        /// <summary />
        public static int FanastyPoints(this NFLPlayer p)
        {
            int points = 0;

            foreach (Game game in p.GameLog)
            {
                points += game.GetFanastyPoints();
            }

            return points;
        }

        /// <summary />
        public static int FanastyPointsInRecentGames(this NFLPlayer p, int gameCount)
        {
            int points = 0;
            
            IEnumerable<Game> games = (from g in p.GameLog
                                       orderby g.Week descending
                                       select g);

            int i = 0;
            foreach (Game game in games)
            {
                i += 1;
                points += game.GetFanastyPoints();

                if (i >= gameCount)
                {
                    // once we have process the requested games, we will bail out.
                    break;
                }
            }

            return points;
        }

        public static List<double> FanastyPointsPerGame(this NFLPlayer p)
        {
            List<double> gameScores = p.GameLog
                                       .Select(g => g.GetFanastyPoints())
                                       .Select(Convert.ToDouble)
                                       .ToList();
            return gameScores;
        }

        /// <summary />
        public static int TotalBonuses(this NFLPlayer player)
        {
            int bonuses = 0;

            foreach (Game game in player.GameLog)
            {
                Passing p = game.Passing;
                if (p != null)
                {
                    bonuses += CountBonuses(p.YDS, 300, 100);
                }

                Rushing r = game.Rushing;
                if (r != null)
                {
                    bonuses += CountBonuses(r.YDS, 100, 50);
                }

                Receiving c = game.Receiving;
                if (c != null)
                {
                    bonuses += CountBonuses(c.YDS, 100, 50);
                }

                Kicking k = game.Kicking;
                if (k != null)
                {
                    bonuses += k.FGM_50plus;

                    if (k.LONG > 63)
                    {
                        // award 4 bonues for 200 points.
                        bonuses += 4;
                    }
                }
            }

            return bonuses;
        }

        /// <summary />
        public static int GetFanastyPoints(this Game game)
        {
            int points = 0;

            Passing p = game.Passing;
            if (p != null)
            {
                points += p.YDS;
                points += 50 * CountBonuses(p.YDS, 300, 100);
                points -= p.INT * 50;

                // estimate 10 yards per passing TD on average
                points += p.TD * 10;
            }

            Rushing r = game.Rushing;
            if (r != null)
            {
                points += r.YDS;
                points += 50 * CountBonuses(r.YDS, 100, 50);

                // estimate 3 yards per rushing TD on average
                points += r.TD * 3;
            }

            Receiving c = game.Receiving;
            if (c != null)
            {
                points += c.YDS;
                points += 50 * CountBonuses(c.YDS, 100, 50);

                // estimate 10 yards per receiving TD on average
                points += c.TD * 10;
            }

            Fumbles f = game.Fumbles;
            if (f != null)
            {
                points -= f.FUM * 25;
            }

            Kicking k = game.Kicking;
            if (k != null)
            {
                // calculate the points for all field goals awarding
                // points per yard of a field goal and subtracting the
                // points of a missed fieldgoal.
                points += k.FGM_01to19 * 15;
                points -= (k.FGA_01to19 - k.FGM_01to19) * 15;

                points += k.FGM_20to29 * 25;
                points -= (k.FGA_20to29 - k.FGM_20to29) * 25;

                points += k.FGM_30to39 * 35;
                points -= (k.FGA_30to39 - k.FGM_30to39) * 35;

                points += k.FGM_40to49 * 45;
                points -= (k.FGA_40to49 - k.FGM_40to49) * 45;

                points += k.FGM_50plus * 50;
                points -= (k.FGA_50plus - k.FGM_50plus) * 50;

                // provide 50 bonus points for FG's over 50 yards
                points += k.FGM_50plus * 50;

                if (k.LONG >= 63)
                {
                    // 200 point bonus for the nfl record
                    points += 200;
                }

                // calculate 20 points for each extra point made and 
                // subtract 20 points for each point missed
                points += k.XPM * 33;
                points -= (k.XPA - k.XPM) * 33;
            }

            Defense defense = game.Defense;
            if (defense != null)
            {
                //calculate points for interceptions
                points += defense.INT * 50;
                points += defense.YDS;
                if (defense.TD_INT > 0)
                {
                    points += (defense.YDS / defense.TD_INT) * defense.TD_INT;
                }

                // calculate points for fumble recoveries
                points += defense.REC * 25;
                points += defense.TD_FUM * 25;

                // calculate points for sacks
                points += defense.SACK * 10;
            }

            return points;
        }

        /// <summary />
        private static bool IsDNP(this Game game)
        {
            return game.Passing == null &&
                   game.Rushing == null &&
                   game.Receiving == null &&
                   game.Fumbles == null &&
                   game.Kicking == null &&
                   game.Defense == null;
        }

        /// <summary />
        private static int CountBonuses(int yds, int min, int step)
        {
            int bonuses = 0;

            if (yds >= min)
            {
                // one bonus for meeting the minimum
                bonuses = 1;

                // one bonus for each additional step
                bonuses += (yds - min) / step;
                
            }

            return bonuses;
        }
    }
}
