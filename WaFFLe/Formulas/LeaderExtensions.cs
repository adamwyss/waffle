
namespace WaFFL.Evaluation
{
    /// <summary />
    public static class LeaderExtensions
    {
        /// <summary />
        public static int Estimate_Points(this ESPNDefenseLeaders td)
        {
            int points = 0;

            //calculate points for interceptions
            points += td.INT * 50;
            points += td.YDS;
            if (td.TD_INT > 0)
            {
                points += (td.YDS / td.TD_INT) * td.TD_INT;
            }

            // calculate points for fumble recoveries
            points += td.REC * 25;

            td.TD_FUM += td.TD_FUM * 25;

            // calculate points for sacks
            points += td.SACK * 10;

            return points;
        }
    }
}
