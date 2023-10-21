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
                    // unable to determine bonuses for kicking yet
                }

                Defense dst = game.Defense;
                if (dst != null)
                {
                    // unable to determine bonuses for defense yet
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

                // In 2016, the average kick was from 37.7 yards away; the average
                // successful kick was from 36.2 yards out - while the average miss was from 46.2 yards away.

                points += k.FGM * 38;
                points -= (k.FGA - k.FGM) * 46;

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
                points += defense.YDS_INT;
                if (defense.TD_INT > 0)
                {
                    // if we have a touchdown, one point per yard avaraged
                    points += (defense.YDS_INT / defense.TD_INT) * defense.TD_INT;
                }

                // calculate points for fumble recoveries
                points += defense.FUM * 25;
                if (defense.TD_FUM > 0)
                {
                    // if we have a touchdown, one point per yard averaged
                    points += (defense.YDS_FUM / defense.TD_FUM) * defense.TD_FUM;
                }               

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

        /// <summary />
        public static bool IsRelevant(this NFLPlayer player)
        {
            int games = player.GameLog.Count(g => !g.IsDNP());
            if (games == 0)
                return false;

            int touches = player.GameLog.Sum(g => g.TotalTouches());
            return touches / games > 3;
        }

        /// <summary />
        private static int TotalTouches(this Game game)
        {
            if (game.Defense != null)
            {
                // defense has large number of touches.
                return 100;
            }

            int p = game.Passing != null ? game.Passing.ATT : 0;
            int r = game.Rushing != null ? game.Rushing.CAR : 0;
            int c = game.Receiving != null ? game.Receiving.REC : 0;
            int k = game.Kicking != null ? game.Kicking.XPA + game.Kicking.FGA : 0;
            return p + r + c + k*5;
        }

    }
}
