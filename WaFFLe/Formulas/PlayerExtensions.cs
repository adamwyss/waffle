using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

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
                    bonuses += CountYardageBonuses(p.YDS, 300, 100);
                    bonuses += CountScoringDistanceBonuses(p.TD_YDS, 50);
                }

                Rushing r = game.Rushing;
                if (r != null)
                {
                    bonuses += CountYardageBonuses(r.YDS, 100, 50);
                    bonuses += CountScoringDistanceBonuses(r.TD_YDS, 50);
                }

                Receiving c = game.Receiving;
                if (c != null)
                {
                    bonuses += CountYardageBonuses(c.YDS, 100, 50);
                    bonuses += CountScoringDistanceBonuses(c.TD_YDS, 50);
                }

                Kicking k = game.Kicking;
                if (k != null)
                {
                    bonuses += CountScoringDistanceBonuses(k.FG_YDS, 50, 67, 4);
                }

                Defense dst = game.Defense;
                if (dst != null)
                {
                    bonuses += CountScoringDistanceBonuses(dst.TD_YDS, 50);
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
                // 1 point per yard; BONUS: 300yds = 50pts, 400yds = 100pts, 500yds = 150pts, etc.
                points += p.YDS;
                points += 50 * CountYardageBonuses(p.YDS, 300, 100);

                // Touchdown(TD) = 1 point per yard; BONUS: 50 + yds = 50pts
                points += p.TD_YDS?.Sum() ?? 0;
                points += 50 * CountScoringDistanceBonuses(p.TD_YDS, 50);

                // Two-Point Conversion = 25pts
                points += p.TWO_PT_CONV * 25;

                // Interception = -50pts
                points -= p.INT * 50;
            }

            Rushing r = game.Rushing;
            if (r != null)
            {
                // 1 point per yard; BONUS: 100yds = 50pts, 150yds = 100pts, 200yds = 150pts, etc.
                points += r.YDS;
                points += 50 * CountYardageBonuses(r.YDS, 100, 50);

                // Touchdown(TD) = 1 point per yard; BONUS: 50 + yds = 50pts
                points += r.TD_YDS?.Sum() ?? 0;
                points += 50 * CountScoringDistanceBonuses(r.TD_YDS, 50);

                // Two - Point Conversion = 25pts
                points += r.TWO_PT_CONV * 25;
            }

            Receiving c = game.Receiving;
            if (c != null)
            {
                // 1 point per yard; BONUS: 100yds = 50pts, 150yds = 100pts, 200yds = 150pts, etc.
                points += c.YDS;
                points += 50 * CountYardageBonuses(c.YDS, 100, 50);

                // Touchdown(TD) = 1 point per yard; BONUS: 50 + yds = 50pts
                points += c.TD_YDS?.Sum() ?? 0;
                points += 50 * CountScoringDistanceBonuses(c.TD_YDS, 50);

                // Two - Point Conversion = 25pts
                points += c.TWO_PT_CONV * 25;
            }

            // Fumble Lost = -25pts
            Fumbles f = game.Fumbles;
            if (f != null)
            {
                points -= f.LOST * 25;
            }

            Kicking k = game.Kicking;
            if (k != null)
            {
                // Extra Point(XP) = 33pts
                points += k.XPM * 33;

                // Field Goal(FG) = 1 point per yard; BONUS: 50 + yds = 50pts, 67 + yds = 200pts
                points += k.FG_YDS?.Sum() ?? 0;
                points += 50 * CountScoringDistanceBonuses(k.FG_YDS, 50, 67, 4); // long bonus: 4 x 50 = 200

                // Missed XP = -33pts
                points -= (k.XPA - k.XPM) * 33;

                // Missed FG = minus 1 point per yard(-1x)
                points -= (k.FGA - k.FGM) * 46;

                // In 2016, the average kick was from 37.7 yards away; the average
                // successful kick was from 36.2 yards out - while the average miss was from 46.2 yards away.
            }

            Defense dst = game.Defense;
            if (dst != null)
            {
                // Interception = 50pts; Interception Return = 1 point per yard
                points += dst.INT * 50;
                points += dst.YDS_INT;

                // Fumble Recovery = 25pts
                points += dst.FUM * 25;

                // Sack = 10pts
                points += dst.SACK * 10;

                // Safety = 50pts
                points += dst.SAFETY * 50;

                // TD = 1 point per yard; BONUS: 50 + yds = 50pts
                points += dst.TD_YDS?.Sum() ?? 0;
                points += 50 * CountScoringDistanceBonuses(dst.TD_YDS, 50);

                // Blocked Kick = 50pts
                // TODO

                // Special Teams TD = 1 point per yard(NO Bonus for 50 + yds)
                points += dst.TD_ST_YDS?.Sum() ?? 0;
            }

            return points;
        }

        private static void AppendGameYards(this StringBuilder sb, int yds, string type, int min, int step)
        {
            sb.Append(" ");
            sb.AppendFormat("{0}{1}", yds, type);

            int bonuses = CountYardageBonuses(yds, min, step);
            if (bonuses > 1)
            {
                sb.AppendFormat("{{{0}b}} ", bonuses);
            }
            else if (bonuses > 0)
            {
                sb.Append("{b}");
            }
        }

        private static void AppendScoringYards(this StringBuilder sb, int yds, string type)
        {
            sb.AppendFormat("{0}{1}", yds, type);
            if (yds > 50)
            {
                sb.Append("{b}");
            }
        }

        private static void AppendScoringCount(this StringBuilder sb, int count, string type)
        {
            if (count > 1)
            {
                sb.Append(count);
            }
            sb.Append(type);
        }

        public static string GetFanastyPointsDetails(this Game game)
        {
            StringBuilder sb = new StringBuilder();

            var p = game.Passing;
            if (p != null)
            {
                if (p.YDS != 0)
                {
                    sb.Append(" ");
                    sb.AppendGameYards(p.YDS, "p", 300, 100);
                    sb.Append(",");
                }

                if (p.TD_YDS != null)
                {
                    foreach (int td in p.TD_YDS)
                    {
                        sb.Append(" ");
                        sb.AppendScoringYards(td, "tdp");
                        sb.Append(",");
                    }
                }

                if (p.TWO_PT_CONV > 0)
                {
                    sb.Append(" ");
                    sb.AppendScoringCount(p.TWO_PT_CONV, "twoptconvp");
                    sb.Append(",");
                }

                if (p.INT > 0)
                {
                    sb.Append(" ");
                    sb.AppendFormat("{0}int", p.INT);
                    sb.Append(",");
                }
            }

            var r = game.Rushing;
            if (r != null)
            {
                if (r.YDS != 0)
                {
                    sb.Append(" ");
                    sb.AppendGameYards(r.YDS, "r", 100, 50);
                    sb.Append(",");
                }

                if (r.TD_YDS != null)
                {
                    foreach (int td in r.TD_YDS)
                    {
                        sb.Append(" ");
                        sb.AppendScoringYards(td, "tdr");
                        sb.Append(",");
                    }
                }

                if (r.TWO_PT_CONV > 0)
                {
                    sb.Append(" ");
                    sb.AppendScoringCount(r.TWO_PT_CONV, "twoptconvr");
                    sb.Append(",");
                }
            }

            var c = game.Receiving;
            if (c != null)
            {
                if (c.YDS != 0)
                {
                    sb.Append(" ");
                    AppendGameYards(sb, c.YDS, "c", 100, 50);
                    sb.Append(",");
                }

                if (c.TD_YDS != null)
                {
                    foreach (int td in c.TD_YDS)
                    {
                        sb.Append(" ");
                        AppendScoringYards(sb, td, "tdc");
                        sb.Append(",");
                    }
                }

                if (c.TWO_PT_CONV > 0)
                {
                    sb.Append(" ");
                    sb.AppendScoringCount(c.TWO_PT_CONV, "twoptconvp");
                    sb.Append(",");
                }
            }

            var fum = game.Fumbles;
            if (fum != null && fum.LOST > 0)
            {
                sb.Append(" ");
                sb.AppendScoringCount(fum.LOST, "fum");
                sb.Append(",");
            }

            var k = game.Kicking;
            if (k != null)
            {
                if (k.XPM > 0)
                {
                    sb.Append(" ");
                    sb.AppendScoringCount(k.XPM, "xp");
                    sb.Append(",");
                }

                if (k.FG_YDS != null)
                {
                    foreach (int fg in k.FG_YDS)
                    {
                        sb.Append(" ");
                        sb.AppendFormat("{0}fg", fg);

                        int bonuses = CountScoringDistanceBonuses(new List<int> { fg }, 50, 67, 4);
                        if (bonuses > 1)
                        {
                            sb.AppendFormat("{{{0}b}} ", bonuses);
                        }
                        else if (bonuses > 0)
                        {
                            sb.Append("{b}");
                        }
                        sb.Append(",");
                    }
                }

                int missedXp = k.XPA - k.XPM;
                if (missedXp > 0)
                {
                    sb.Append(" ");
                    sb.Append("-");
                    sb.AppendScoringCount(missedXp, "xp");
                    sb.Append(",");
                }

                int missedFg = k.FGA - k.FGM;
                if (missedFg > 0)
                {
                    for (int i = 0; i < missedFg; i++)
                    {
                        sb.Append(" ");
                        sb.Append("-46fg");
                    }
                    sb.Append(",");
                }
            }

            var dst = game.Defense;
            if (dst != null)
            {
                if (dst.SACK > 0)
                {
                    sb.Append(" ");
                    sb.AppendFormat("{0}s", dst.SACK);
                    sb.Append(",");
                }

                if (dst.FUM > 0)
                {
                    sb.Append(" ");
                    sb.AppendFormat("{0}fr", dst.FUM);
                    sb.Append(",");
                }

                if (dst.INT > 0)
                {
                    sb.Append(" ");
                    sb.AppendFormat("{0}int {1}yds", dst.INT, dst.YDS_INT);
                    sb.Append(",");
                }

                if (dst.TD_YDS != null)
                {
                    foreach (int td in dst.TD_YDS)
                    {
                        sb.Append(" ");
                        sb.AppendScoringYards(td, "td");
                        sb.Append(",");
                    }
                }

                if (dst.TD_ST_YDS != null)
                {
                    foreach (int td in dst.TD_ST_YDS)
                    {
                        sb.Append(" ");
                        sb.AppendFormat("{0}tdst", td);
                        sb.Append(",");
                    }
                }

                if (dst.SAFETY > 0)
                {
                    sb.Append(" ");
                    sb.AppendScoringCount(dst.SAFETY, "safety");
                    sb.Append(",");
                }
            }
            if (sb.Length > 0)
            {
            sb.Remove(0, 1);
            sb.Length--;
            }
            else
            {
                sb.Append("DNP");
            }

            return sb.ToString();
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
        private static int CountYardageBonuses(int yds, int min, int step)
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
        private static int CountScoringDistanceBonuses(List<int> distances, int threshold, int? premiumThreshold = null, int? premiumBonus = null)
        {
            if (distances == null)
            {
                return 0;
            }

            int bonuses = 0;
            foreach (int distance in distances)
            {
                if (premiumThreshold.HasValue && distance >= premiumThreshold.Value)
                {
                    if (premiumBonus.HasValue)
                    {
                        bonuses += premiumBonus.Value;
                    }
                    else
                    {
                        bonuses += 1;
                    }
                }
                else if (distance >= threshold)
                {
                    bonuses += 1;
                }
            }

            return bonuses;
        }

        /// <summary />
        public static bool IsRelevant(this NFLPlayer player)
        {
            int games = player.GameLog.Count(g => !g.IsDNP());
            if (games == 0)
                return false;            

            return true;
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


        /// <summary />
        public static bool ProbablyPlays(this NFLPlayer player, FanastyPosition position)
        {
            return player.GetPositionOrBestGuess() == position;
        }

        /// <summary />
        public static FanastyPosition GetPositionOrBestGuess(this NFLPlayer player)
        {
            FanastyPosition position = player.Position;
            if (position != FanastyPosition.UNKNOWN)
            {
                return position;
            }

            return player.GuessPosition();
        }

        /// <summary />
        public static FanastyPosition GuessPosition(this NFLPlayer p)
        {
            int passAttempts = p.GameLog.Sum(g => g.Passing != null ? g.Passing.ATT : 0);
            int rushAttempts = p.GameLog.Sum(g => g.Rushing != null ? g.Rushing.CAR : 0);
            int recievingAttempts = p.GameLog.Sum(g => g.Receiving != null ? g.Receiving.REC : 0);
            int kickAttempts = p.GameLog.Sum(g => g.Kicking != null ? g.Kicking.XPA + g.Kicking.FGA : 0);

            if (passAttempts > rushAttempts && recievingAttempts < 2 && kickAttempts == 0)
            {
                return FanastyPosition.QB;
            }
            else if (passAttempts == 0 && recievingAttempts > rushAttempts && kickAttempts == 0)
            {
                return FanastyPosition.WR;
            }
            else if (passAttempts == 0 && rushAttempts > 0 && recievingAttempts < rushAttempts && kickAttempts == 0)
            {
                return FanastyPosition.RB;
            }
            else if (kickAttempts > 0)
            {
                return FanastyPosition.K;
            }

            return p.Position;
        }
    }
}
