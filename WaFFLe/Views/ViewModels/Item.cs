using System.Diagnostics;
using System.Text;

namespace WaFFL.Evaluation
{
    /// <summary />
    [DebuggerDisplay("{PlayerData}")]
    public abstract class Item
    {
        /// <summary />
        public Item(NFLPlayer p)
        {
            this.PlayerData = p;
        }

        /// <summary />
        public NFLPlayer PlayerData { get; private set; }

        /// <summary />
        public bool IsAvailable
        {
            get { return WaFFLRoster.IsActive(this.PlayerData.Name); }
        }

        /// <summary />
        public string Name
        {
            get { return this.PlayerData.Name; }
        }

        /// <summary />
        public string Team
        {
            get
            {
                NFLTeam team = this.PlayerData.Team;
                if (team != null)
                {
                    return team.TeamCode;
                }

                return "n/a";
            }
        }

        /// <summary />
        public string WeekByWeekScores
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                foreach (Game g in this.PlayerData.GameLog)
                {
                    int points = g.GetFanastyPoints();
                    sb.Append(string.Format("{0}::", g.Week));
                    sb.Append(points);
                    sb.Append(", ");
                }

                if (sb.Length > 2)
                    sb.Remove(sb.Length - 2, 2);

                return sb.ToString();
            }
        }
    }
}
